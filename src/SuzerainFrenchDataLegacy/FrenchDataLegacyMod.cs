using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json.Linq;

namespace SuzerainFrenchDataLegacy;

public sealed class FrenchDataLegacyMod : MelonMod
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const BindingFlags StaticFlags =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static FrenchDataLegacyMod current;

    private readonly Dictionary<string, List<JObject>> translations =
        new(StringComparer.Ordinal);

    private bool ready;
    private bool pendingSceneTranslation;
    private int retriesRemaining;
    private bool batchProbeCompleted;

    public override void OnInitializeMelon()
    {
        current = this;

        try
        {
            LoadTranslations();
            PatchLegacyDataManager();
            ready = true;
        }
        catch (Exception exception)
        {
            ready = false;
            LoggerInstance.Error("Impossible d'initialiser la traduction des données : " + exception);
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        pendingSceneTranslation = true;
        retriesRemaining = 600;
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (!TryTranslateStaticData() && !batchProbeCompleted && IsBatchMode())
        {
            batchProbeCompleted = true;
            MethodInfo loadAllData = AccessTools.Method(
                AccessTools.TypeByName("ArticyDataManager"),
                "LoadAllData");

            if (loadAllData != null)
            {
                loadAllData.Invoke(null, null);
                TryTranslateStaticData();
            }
        }
    }

    public override void OnUpdate()
    {
        if (ready && pendingSceneTranslation && retriesRemaining-- > 0)
        {
            TryTranslateStaticData();
        }
    }

    private void LoadTranslations()
    {
        string modDirectory = Path.GetDirectoryName(typeof(FrenchDataLegacyMod).Assembly.Location);
        string translationDirectory = Path.Combine(
            modDirectory,
            "SuzerainTrad",
            "Languages",
            "French",
            "DataTranslations");

        int dictionaryCount = 0;
        int recordCount = 0;

        foreach (string filePath in Directory.GetFiles(translationDirectory, "*.txt"))
        {
            if (JObject.Parse(File.ReadAllText(filePath))["items"] is not JArray items)
            {
                continue;
            }

            dictionaryCount++;

            foreach (JToken item in items)
            {
                if (item is not JObject record)
                {
                    continue;
                }

                string databaseName = (string)record["NameInDatabase"];
                if (string.IsNullOrEmpty(databaseName))
                {
                    continue;
                }

                if (!translations.TryGetValue(databaseName, out List<JObject> matches))
                {
                    matches = new List<JObject>();
                    translations.Add(databaseName, matches);
                }

                matches.Add(record);
                recordCount++;
            }
        }

        LoggerInstance.Msg(
            $"{dictionaryCount} dictionnaires et {recordCount} fiches françaises " +
            "chargés pour l'ancienne architecture.");
    }

    private void PatchLegacyDataManager()
    {
        Type managerType = AccessTools.TypeByName("ArticyDataManager");
        if (managerType == null)
        {
            throw new TypeLoadException("ArticyDataManager est introuvable.");
        }

        MethodInfo postfixMethod = typeof(FrenchDataLegacyMod).GetMethod(
            nameof(AfterDataLoad),
            BindingFlags.Static | BindingFlags.NonPublic);
        HarmonyMethod postfix = new(postfixMethod);

        int patchedMethodCount = 0;
        string[] methodNames =
        {
            "LoadGameSetupData",
            "LoadGameFlowData",
            "LoadAllData",
            "LoadDynamicData",
            "LoadStaticData"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = AccessTools.Method(managerType, methodName);
            if (method == null)
            {
                continue;
            }

            HarmonyInstance.Patch(method, postfix: postfix);
            patchedMethodCount++;
        }

        if (patchedMethodCount == 0)
        {
            throw new MissingMethodException(
                "Aucune méthode de chargement Articy n'a été trouvée.");
        }

        LoggerInstance.Msg(
            $"Adaptateur Amazon actif sur {patchedMethodCount} méthodes de chargement de données.");
    }

    private static void AfterDataLoad()
    {
        current?.TranslateManager(AccessTools.TypeByName("ArticyDataManager"));
    }

    private bool TryTranslateStaticData()
    {
        if (!TranslateManager(AccessTools.TypeByName("ArticyDataManager")))
        {
            return false;
        }

        pendingSceneTranslation = false;
        return true;
    }

    private bool TranslateManager(Type managerType)
    {
        if (!ready || managerType == null)
        {
            return false;
        }

        try
        {
            int translatedFieldCount = 0;
            int translatedRecordCount = 0;
            int inspectedRecordCount = 0;

            foreach (FieldInfo managerField in managerType.GetFields(StaticFlags))
            {
                object value = managerField.GetValue(null);
                if (value == null)
                {
                    continue;
                }

                if (value is IEnumerable enumerable && value is not string)
                {
                    foreach (object record in enumerable)
                    {
                        if (record != null)
                        {
                            inspectedRecordCount++;
                        }

                        int translatedFields = TranslateRecord(record);
                        if (translatedFields > 0)
                        {
                            translatedRecordCount++;
                            translatedFieldCount += translatedFields;
                        }
                    }
                }
                else
                {
                    inspectedRecordCount++;
                    int translatedFields = TranslateRecord(value);
                    if (translatedFields > 0)
                    {
                        translatedRecordCount++;
                        translatedFieldCount += translatedFields;
                    }
                }
            }

            if (translatedFieldCount > 0)
            {
                LoggerInstance.Msg(
                    $"Informations françaises appliquées : {translatedRecordCount} fiches, " +
                    $"{translatedFieldCount} champs visibles.");
            }

            return inspectedRecordCount > 0;
        }
        catch (Exception exception)
        {
            LoggerInstance.Error("Erreur pendant la traduction des données : " + exception);
            return false;
        }
    }

    private int TranslateRecord(object record)
    {
        if (record == null)
        {
            return 0;
        }

        string databaseName = ReadStringMember(record, "NameInDatabase", "nameInDatabase");
        if (string.IsNullOrEmpty(databaseName) ||
            !translations.TryGetValue(databaseName, out List<JObject> matches))
        {
            return 0;
        }

        int translatedFieldCount = 0;

        foreach (FieldInfo field in record.GetType().GetFields(InstanceFlags))
        {
            object target = field.GetValue(record);
            if (target == null || target is string)
            {
                continue;
            }

            string targetTypeName = target.GetType().Name;
            foreach (JObject match in matches)
            {
                if (match.GetValue(targetTypeName, StringComparison.OrdinalIgnoreCase)
                    is not JObject translatedObject)
                {
                    continue;
                }

                translatedFieldCount += ApplyVisibleFields(target, translatedObject);
                break;
            }
        }

        return translatedFieldCount;
    }

    private static int ApplyVisibleFields(object target, JObject source)
    {
        int translatedFieldCount = 0;

        foreach (JProperty property in source.Properties())
        {
            if (!IsVisibleTextField(property.Name))
            {
                continue;
            }

            FieldInfo field = target.GetType().GetField(property.Name, InstanceFlags | BindingFlags.IgnoreCase);
            if (field == null)
            {
                continue;
            }

            if (field.FieldType == typeof(string) && property.Value.Type == JTokenType.String)
            {
                string translation = (string)property.Value;
                string currentValue = (string)field.GetValue(target);

                if (!string.IsNullOrEmpty(translation) && currentValue != translation)
                {
                    field.SetValue(target, translation);
                    translatedFieldCount++;
                }
            }
            else if (
                property.Name.Equals("Keywords", StringComparison.OrdinalIgnoreCase) &&
                field.FieldType == typeof(List<string>) &&
                property.Value is JArray)
            {
                List<string> keywords = property.Value.ToObject<List<string>>();
                if (keywords != null && keywords.Count > 0)
                {
                    field.SetValue(target, keywords);
                    translatedFieldCount++;
                }
            }
        }

        return translatedFieldCount;
    }

    private static bool IsVisibleTextField(string name)
    {
        if (name.EndsWith("Title", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("Text", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("Description", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return name.ToLowerInvariant() switch
        {
            "president" => true,
            "relations" => true,
            "keywords" => true,
            "language" => true,
            "capital" => true,
            "government" => true,
            "gdpworldrank" => true,
            "officiallanguage" => true,
            _ => false
        };
    }

    private static string ReadStringMember(
        object target,
        string propertyName,
        string fieldName)
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            InstanceFlags | BindingFlags.IgnoreCase);

        if (property?.PropertyType == typeof(string))
        {
            return (string)property.GetValue(target, null);
        }

        FieldInfo field = target.GetType().GetField(
            fieldName,
            InstanceFlags | BindingFlags.IgnoreCase);

        return field?.FieldType == typeof(string)
            ? (string)field.GetValue(target)
            : null;
    }

    private static bool IsBatchMode()
    {
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.Equals("-batchmode", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
