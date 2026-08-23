# Suzerain FR — compatibilité Amazon/Legacy

Couche de compatibilité permettant d'utiliser la traduction française communautaire de Suzerain avec l'ancienne version Windows distribuée par Amazon Games.

## Compatibilité

- Suzerain `2.0_release_0.6`
- Windows, version Amazon Games
- MelonLoader `0.7.2`

Ce projet n'est pas destiné à l'architecture récente de la version Steam.

## Contenu traduit

- dialogues et choix narratifs ;
- interface générale ;
- journaux, rapports et actualités ;
- codex, situations et politiques ;
- décisions, décrets, résumés, rappels et info-bulles.

Le test de référence charge 96 libellés d'interface, 57 dictionnaires de données contenant 9 833 fiches et 58 473 entrées de dialogue.

## État de la publication

Le dépôt contient le code des deux adaptateurs Amazon/Legacy. Les textes français et `SuzerainTrad.dll` proviennent du projet [Suzerain French Translation Mod](https://github.com/Barbecuecitron/Suzerain-French-Translation-Mod) et ne sont pas versionnés ici.

Le dépôt amont ne présentant pas de licence explicite au moment de cette adaptation, aucune archive publique contenant ces éléments ne doit être publiée sans l'accord de son auteur. Voir [docs/PERMISSIONS.md](docs/PERMISSIONS.md).

## Installation d'une version publiée

1. Fermez Suzerain.
2. Installez [MelonLoader 0.7.2](https://github.com/LavaGang/MelonLoader/releases) dans le dossier de Suzerain.
3. Décompressez l'archive de cette adaptation dans le dossier du jeu en conservant l'arborescence `Mods`.
4. Lancez le jeu. Le premier démarrage avec MelonLoader peut être plus long.

Il est recommandé de sauvegarder les parties avant d'installer un mod.

## Compilation

Prérequis : SDK .NET 8 ou plus récent et une installation locale de la version Amazon de Suzerain.

```powershell
.\scripts\build.ps1 -GamePath "C:\chemin\vers\Suzerain"
```

Les DLL sont produites dans `artifacts/build`.

## Création d'une archive

Après avoir obtenu les autorisations nécessaires et installé localement la traduction complète :

```powershell
.\scripts\package.ps1 `
  -GamePath "C:\chemin\vers\Suzerain" `
  -IHaveRedistributionPermission
```

Cette commande crée une archive et son empreinte SHA-256 dans `artifacts/release`. Le commutateur explicite empêche une republication accidentelle des textes communautaires.

## Désinstallation

Supprimez uniquement les éléments suivants du dossier `Mods` :

- `SuzerainFrenchLegacy.dll` ;
- `SuzerainFrenchDataLegacy.dll` ;
- `SuzerainTrad.dll` ;
- le dossier `SuzerainFrenchLegacy` ;
- le dossier `SuzerainTrad`.

Ne supprimez pas le dossier `Mods` entier si d'autres mods y sont installés.

## Crédits

- Traduction française : Barbecuecitron et les contributeurs du projet de traduction.
- Adaptation Amazon/Legacy : contributeurs de ce dépôt.
- MelonLoader et Harmony : leurs auteurs respectifs.

Ce projet est non officiel, gratuit et sans affiliation avec Torpor Games, Fellow Traveller, Amazon Games ou Unity Technologies. Aucun fichier original du jeu n'est inclus.
