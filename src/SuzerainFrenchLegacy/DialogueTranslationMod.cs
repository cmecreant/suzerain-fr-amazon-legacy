using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;
using Newtonsoft.Json;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace SuzerainFrenchLegacy;

public sealed class DialogueTranslationMod : MelonMod
{
    private readonly Dictionary<long, TranslationEntry> translations = new();
    private bool ready;
    private bool appliedForScene;
    private int retriesRemaining;

    public override void OnInitializeMelon()
    {
        try
        {
            string modDirectory = Path.GetDirectoryName(typeof(DialogueTranslationMod).Assembly.Location);
            string translationPath = Path.Combine(
                modDirectory,
                "SuzerainFrenchLegacy",
                "legacy_dialogues_fr.json");

            TranslationFile translationFile = JsonConvert.DeserializeObject<TranslationFile>(
                File.ReadAllText(translationPath, Encoding.UTF8));

            if (translationFile?.entries == null)
            {
                throw new InvalidDataException("Le fichier de traduction est vide.");
            }

            foreach (TranslationEntry entry in translationFile.entries)
            {
                translations[MakeKey(entry.c, entry.e)] = entry;
            }

            ready = translations.Count > 0;
            LoggerInstance.Msg($"{translations.Count} entrées de dialogues français chargées.");
        }
        catch (Exception exception)
        {
            ready = false;
            LoggerInstance.Error("Impossible de charger les dialogues français : " + exception);
        }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        appliedForScene = false;
        retriesRemaining = 600;
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        TryApply();
    }

    public override void OnUpdate()
    {
        if (ready && !appliedForScene && retriesRemaining-- > 0)
        {
            TryApply();
        }
    }

    private void TryApply()
    {
        if (!ready || appliedForScene)
        {
            return;
        }

        try
        {
            DialogueDatabase database = DialogueManager.MasterDatabase;
            if ((UnityEngine.Object)database == null ||
                database.conversations == null ||
                database.conversations.Count == 0)
            {
                return;
            }

            int matchedEntries = 0;
            int translatedDialogues = 0;
            int translatedChoices = 0;

            foreach (Conversation conversation in database.conversations)
            {
                if (conversation?.dialogueEntries == null)
                {
                    continue;
                }

                foreach (DialogueEntry dialogueEntry in conversation.dialogueEntries)
                {
                    if (dialogueEntry == null ||
                        !translations.TryGetValue(
                            MakeKey(((Asset)conversation).id, dialogueEntry.id),
                            out TranslationEntry translation))
                    {
                        continue;
                    }

                    matchedEntries++;

                    if (!string.IsNullOrEmpty(translation.d))
                    {
                        dialogueEntry.DialogueText = translation.d;
                        translatedDialogues++;
                    }

                    if (!string.IsNullOrEmpty(translation.m))
                    {
                        dialogueEntry.MenuText = translation.m;
                        translatedChoices++;
                    }
                }
            }

            if (matchedEntries > 0)
            {
                appliedForScene = true;
                LoggerInstance.Msg(
                    $"Dialogues français appliqués : {matchedEntries} entrées " +
                    $"({translatedDialogues} textes, {translatedChoices} choix).");
            }
        }
        catch (Exception exception)
        {
            LoggerInstance.Error("Erreur pendant l'application des dialogues français : " + exception);
            appliedForScene = true;
        }
    }

    private static long MakeKey(int conversationId, int entryId)
    {
        return ((long)conversationId << 32) | (uint)entryId;
    }
}

public sealed class TranslationFile
{
    public List<TranslationEntry> entries;
}

public sealed class TranslationEntry
{
    public int c;
    public int e;
    public string d;
    public string m;
}
