# FTool by Tratox

![FTool Logo](https://img.shields.io/badge/FTool-v1.0-red) ![.NET Framework](https://img.shields.io/badge/.NET_Framework-4.7+-blue) ![License](https://img.shields.io/badge/License-MIT-green)

**FTool by Tratox** est un outil d'automatisation sophistiqué conçu spécialement pour le jeu **Flyff** (Fly For Fun). Cette application permet de gérer jusqu'à 20 spammeurs simultanés avec une interface moderne inspirée d'Opera GX.

## 🎮 Fonctionnalités

### ✨ Spammeurs Multiples
- **20 spammeurs simultanés** organisés en 4 onglets de 5 spammeurs chacun
- **Détection automatique** des fenêtres Flyff (processus Neuz.exe)
- **Configuration individuelle** pour chaque spammeur
- **Statistiques en temps réel** avec compteur d'actions

### ⚡ Hotkeys Globales
- **Raccourcis clavier système** fonctionnant même quand l'application n'est pas au premier plan
- **Master controls** : Start/Stop tous les spammeurs d'un coup
- **Contrôles individuels** : Une hotkey par spammeur
- **Configuration facile** via interface graphique

### 🎯 Options de Spam
- **Touches F** : F1 à F9 pour les sorts/compétences
- **Barre de compétences** : Touches 1 à 9 pour les raccourcis
- **Intervalles personnalisables** : De 0 seconde (spam continu) à plusieurs minutes
- **Combinaisons de touches** : F-Key + Skill simultanément

### 🎨 Interface Gaming Moderne
- **Design Opera GX** avec thème sombre et accents néon
- **Animations fluides** et effets visuels
- **Interface sans bordure** avec contrôles personnalisés
- **Redimensionnement libre** et déplacement par glisser-déposer

## 📋 Prérequis

- **Windows 10/11**
- **.NET Framework 4.7.2** ou supérieur
- **Flyff** installé et lancé (processus Neuz.exe détectable)
- **Droits administrateur** recommandés pour les hotkeys globales

## 🚀 Installation

1. **Télécharger** la dernière version depuis les [Releases](../../releases)
2. **Extraire** l'archive dans un dossier de votre choix
3. **Lancer** `FToolByTratox.exe`
4. L'application créera automatiquement le fichier de configuration `FToolByTratox_Settings.ini`

## 📖 Guide d'utilisation

### 1️⃣ Configuration de base

1. **Lancez Flyff** et connectez-vous à vos personnages
2. **Ouvrez FTool** - L'application détecte automatiquement les fenêtres Flyff
3. **Sélectionnez une fenêtre** dans la liste déroulante de chaque spammeur
4. **Configurez les touches** :
   - **INT** : Intervalle en secondes (0 = spam continu)
   - **F-KEY** : Touche de fonction (F1-F9)
   - **SKILL** : Touche de la barre de compétences (1-9)

### 2️⃣ Contrôles

#### Boutons principaux
- **▶️ START** : Démarre le spammeur individuel
- **⏹️ STOP** : Arrête le spammeur individuel  
- **🚀 START ALL** : Démarre tous les spammeurs configurés
- **⏹️ STOP ALL** : Arrête tous les spammeurs actifs
- **🔄** : Remet à zéro la configuration du spammeur

#### États visuels
- **🟢 ACTIVE** : Spammeur en cours d'exécution
- **⚫ IDLE** : Spammeur arrêté
- **Actions: X** : Nombre d'actions effectuées

### 3️⃣ Configuration des Hotkeys

1. **Allez dans l'onglet ⚙️ Settings**
2. **Master Controls** :
   - Définissez une hotkey pour démarrer tous les spammeurs
   - Définissez une hotkey pour arrêter tous les spammeurs
3. **Individual Spammer Hotkeys** :
   - Cliquez sur le bouton de hotkey de chaque spammeur
   - Appuyez sur la combinaison souhaitée (ex: Ctrl+F1)
   - Confirmez ou annulez

#### Combinaisons supportées
- **Ctrl** + Touche
- **Alt** + Touche
- **Shift** + Touche
- **Win** + Touche
- **Combinaisons multiples** (ex: Ctrl+Alt+F1)

### 4️⃣ Surveillance et Statistiques

L'interface affiche en temps réel :
- **Nombre de processus Flyff** détectés
- **État de chaque spammeur** (ACTIVE/IDLE)
- **Compteur d'actions** par spammeur
- **Hotkeys configurées** pour chaque spammeur

## ⚙️ Fichier de Configuration

L'application sauvegarde automatiquement dans `FToolByTratox_Settings.ini` :

```ini
[General]
MasterStartHotKey=Ctrl + F10
MasterStopHotKey=Ctrl + F11
EnableGlobalHotKeys=True

[HotKeys]
Spammer1=Ctrl + F1
Spammer2=Ctrl + F2

[Spammer1]
WindowTitle=Flyff - Character1
Interval=1
FKey=F1
Skill=1

[Spammer2]
WindowTitle=Flyff - Character2  
Interval=2
FKey=F2
Skill=2
```

## 🛡️ Sécurité et Bonnes Pratiques

### ⚠️ Avertissements
- **Utilisez cet outil de manière responsable**
- **Respectez les règles du serveur** Flyff sur lequel vous jouez
- **L'automatisation peut être interdite** sur certains serveurs
- **L'auteur n'est pas responsable** des sanctions éventuelles

### 🔒 Sécurité Technique
- **Aucune injection de code** dans le processus Flyff
- **Communications via API Windows** standards (SendMessage/PostMessage)
- **Aucune modification de mémoire** du jeu
- **Détection basée sur les noms de fenêtres** uniquement

### 📝 Recommandations
- **Testez sur un serveur privé** avant utilisation
- **Utilisez des intervalles raisonnables** (évitez le spam trop rapide)
- **Surveillez votre utilisation** pour éviter la détection
- **Fermez l'outil** si vous ne l'utilisez pas

## 🏗️ Architecture Technique

### 📁 Structure du Code

```
FToolByTratox/
├── Program.cs                 # Point d'entrée principal
├── MainForm.cs               # Interface principale
├── SpammerData.cs           # Gestion des données de spammeur
├── SettingsData.cs          # Configuration globale
├── HotkeyCapture.cs         # Capture des raccourcis clavier
└── UI Components/
    ├── GamingButton.cs      # Boutons personnalisés
    ├── NeonButton.cs        # Boutons avec effets néon
    ├── GamingTextBox.cs     # Champs de texte gaming
    ├── GamingComboBox.cs    # Listes déroulantes
    ├── GamingTabControl.cs  # Onglets personnalisés
    └── Custom Panels/       # Panneaux et indicateurs
```

### 🔧 APIs Utilisées

| API Windows | Usage |
|-------------|--------|
| `SendMessage` | Envoi de touches au jeu |
| `PostMessage` | Envoi asynchrone de commandes |
| `RegisterHotKey` | Enregistrement des raccourcis globaux |
| `GetForegroundWindow` | Détection de la fenêtre active |
| `FindWindow` | Recherche des fenêtres Flyff |

### 🎨 Système de Thèmes

- **Palette Opera GX** : Rouge néon (#FF4A4D), Bleu (#42A5F5), Violet (#AB47BC)
- **Arrière-plans dégradés** avec LinearGradientBrush
- **Effets de glow** avec transparence alpha
- **Animations de pulsation** sur les indicateurs d'état

## 🔄 Processus de Fonctionnement

### 1. Détection des Processus
```csharp
Process[] processes = Process.GetProcessesByName("Neuz");
// Scan toutes les 2 secondes pour détecter les nouvelles fenêtres
```

### 2. Envoi de Touches
```csharp
// Envoi d'une touche F
PostMessage(windowHandle, WM_KEYDOWN, F1_VIRTUAL_KEY, IntPtr.Zero);

// Envoi d'une touche numérique
PostMessage(windowHandle, WM_KEYDOWN, KEY_1_VIRTUAL_KEY, IntPtr.Zero);
```

### 3. Gestion des Timers
- **Timer principal** : Exécution du spam selon l'intervalle configuré
- **Timer de surveillance** : Vérification des fenêtres (2s)
- **Timer de statistiques** : Mise à jour de l'affichage (1s)

### 4. Hotkeys Globales
```csharp
RegisterHotKey(this.Handle, hotkeyId, MOD_CONTROL, VK_F1);
// Écoute des messages WM_HOTKEY dans WndProc
```

## 🐛 Dépannage

### Problèmes Courants

#### L'application ne détecte pas Flyff
- ✅ Vérifiez que le processus se nomme bien `Neuz.exe`
- ✅ Lancez FTool **après** avoir ouvert Flyff
- ✅ Redémarrez FTool si Flyff était fermé

#### Les hotkeys ne fonctionnent pas
- ✅ Lancez en tant qu'**administrateur**
- ✅ Vérifiez que les hotkeys ne sont pas utilisées par d'autres applications
- ✅ Désactivez/réactivez les hotkeys dans les paramètres

#### Le spam ne fonctionne pas
- ✅ Vérifiez que la **fenêtre Flyff est sélectionnée** correctement
- ✅ Testez avec un **intervalle plus long** (2-3 secondes)
- ✅ Vérifiez que les **touches F/Skills sont bien configurées** dans le jeu

#### Interface qui bug
- ✅ **Redimensionnez la fenêtre** si l'affichage est incorrect
- ✅ **Relancez l'application** en cas de problème persistant
- ✅ Supprimez `FToolByTratox_Settings.ini` pour reset la config

### Logs de Debug

Pour activer les logs détaillés, modifiez le code :
```csharp
Debug.WriteLine($"Spammer {index} started - Window: {window}");
// Les logs apparaissent dans la console de débogage de Visual Studio
```

## 📊 Limitations Techniques

### Performance
- **Maximum 20 spammeurs** simultanés (limite arbitraire pour les performances)
- **Intervalle minimum conseillé** : 100ms (0.1 seconde)
- **Consommation mémoire** : ~15-25 MB selon le nombre de spammeurs actifs

### Compatibilité
- **Windows uniquement** (APIs Win32)
- **Flyff clients standard** uniquement (processus Neuz.exe)
- **Résolution minimale** : 1024x768

### Restrictions
- **Une fenêtre par spammeur** (pas de multi-targeting)
- **Touches simples uniquement** (pas de combinaisons complexes dans le jeu)
- **Pas de détection de déconnexion** automatique

## 🤝 Contribution

### Améliorations Possibles
- [ ] **Support multi-langues** (EN, FR, ES, etc.)
- [ ] **Thèmes personnalisables** (couleurs, styles)
- [ ] **Profils de configuration** sauvegardables
- [ ] **Détection de déconnexion** avec reconnexion auto
- [ ] **Support d'autres jeux** MMORPG
- [ ] **Interface web** pour contrôle à distance
- [ ] **Logs détaillés** avec historique
- [ ] **Statistiques avancées** (graphiques, rapports)

### Comment Contribuer
1. **Fork** le projet
2. **Créez une branche** pour votre feature (`git checkout -b feature/AmazingFeature`)
3. **Committez** vos changements (`git commit -m 'Add AmazingFeature'`)
4. **Push** vers la branche (`git push origin feature/AmazingFeature`)
5. **Ouvrez une Pull Request**

## 📄 Licence

Ce projet est sous licence **MIT** - voir le fichier [LICENSE](LICENSE) pour plus de détails.

```
MIT License

Copyright (c) 2024 Tratox

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 👨‍💻 Auteur

**Tratox** - *Développeur principal*
- 🎮 Passionné de gaming et d'automatisation
- 💻 Spécialiste C# / .NET Framework
- 🎨 Designer d'interfaces gaming modernes

## 🙏 Remerciements

- **Communauté Flyff** pour les tests et retours
- **Microsoft** pour le .NET Framework
- **Opera GX** pour l'inspiration du design
- **Tous les contributeurs** qui ont aidé à améliorer ce projet

---

<div align="center">

**⭐ Si ce projet vous a été utile, n'hésitez pas à lui donner une étoile ! ⭐**

*Made with ❤️ for the Flyff community*

</div>
