# Contribuer à MyBreadboard

Merci de votre intérêt pour MyBreadboard ! Ce document décrit comment préparer un environnement local, compiler et lancer l’application, puis proposer une contribution.

## Présentation du projet

MyBreadboard est une application de bureau Windows développée en C# avec WPF et XAML. L’application permet de dessiner des schémas électriques simples. Les fils sont des éléments visuels : ils ne modélisent pas de connexions électriques ni de simulation de circuit.

## Prérequis

- Windows 10 ou Windows 11. WPF nécessite Windows.
- Git.
- Le SDK .NET 8.
- Au choix :
  - Visual Studio 2022 avec la charge de travail **Développement .NET Desktop** ; ou
  - PowerShell ou un terminal Windows avec le SDK .NET installé.

Vérifiez l’installation du SDK avec :

```powershell
dotnet --list-sdks
```

La liste doit contenir une version `8.0.x`.

## Récupérer le projet

Clonez le dépôt et placez-vous dans son répertoire :

```powershell
git clone https://github.com/Topsxi-Developpers/MyBreadboard.git
cd MyBreadboard
```

Pour travailler sur une contribution, créez une branche dédiée à partir de la branche par défaut du dépôt :

```powershell
git switch -c feature/description-courte
```

Adaptez le nom de branche à votre changement, par exemple `fix/save-dialog` ou `docs/setup`.

## Restaurer, compiler et lancer

Depuis la racine du dépôt :

```powershell
dotnet restore .\MyBreadboard.sln
dotnet build .\MyBreadboard.sln
dotnet run --project .\src\MyBreadboard.App\MyBreadboard.App.csproj
```

Pour compiler une version Release :

```powershell
dotnet build .\MyBreadboard.sln --configuration Release
```

Avec Visual Studio, ouvrez `MyBreadboard.sln`, laissez Visual Studio restaurer les dépendances, puis lancez l’application avec **F5**.

## Vérifications avant de proposer un changement

1. Compilez la solution avec `dotnet build .\MyBreadboard.sln`.
2. Lancez l’application et vérifiez manuellement les fonctionnalités concernées. Pour les changements d’interface, contrôlez notamment les interactions souris et clavier.
3. Si votre changement touche aux fichiers `.mybrd`, testez l’enregistrement puis la réouverture d’un schéma, y compris les cas pertinents de compatibilité.
4. Examinez votre diff et retirez les fichiers générés, les modifications sans rapport et les traces de débogage.

La solution actuelle ne contient pas de projet de tests automatisés. Si vous ajoutez des tests, documentez leur exécution et gardez-les reproductibles.

## Bonnes pratiques de contribution

- Vérifiez d’abord les issues et discussions existantes pour éviter les doublons. Pour un changement important, ouvrez une issue ou discutez de l’approche avant de commencer.
- Gardez chaque contribution ciblée et de taille raisonnable.
- Respectez les conventions déjà présentes dans le code : C#, WPF/XAML, espaces de noms existants et annotations de nullabilité.
- Évitez d’ajouter une dépendance si la plateforme ou le framework fournit déjà une solution adaptée.
- Mettez à jour la documentation lorsqu’un changement modifie le comportement ou les instructions d’utilisation.
- N’introduisez pas dans les données du schéma une signification électrique que l’application ne prend pas encore en charge ; les fils actuels sont uniquement visuels.

## Commits et pull requests

Faites des commits clairs, décrivant le changement. Par exemple :

```text
Add annotation text editing
Fix document save path handling
Document local setup
```

Avant d’ouvrir une pull request :

- vérifiez que la branche est à jour avec la branche par défaut ;
- vérifiez que la solution compile ;
- fournissez un résumé du changement et sa motivation ;
- indiquez les vérifications manuelles ou automatisées effectuées ;
- ajoutez des captures d’écran pour les changements d’interface lorsque cela aide à les comprendre ;
- signalez explicitement les limitations ou les tâches de suivi.

Une pull request doit se concentrer sur un sujet. Les retours de revue peuvent nécessiter des ajustements avant l’intégration.

## Signaler un problème

Lors d’un signalement, indiquez si possible :

- les étapes pour reproduire le problème ;
- le résultat attendu et le résultat observé ;
- votre version de Windows et du SDK .NET ;
- le message d’erreur complet ou une capture d’écran ;
- si le problème concerne un fichier `.mybrd`, un exemple minimal reproductible ne contenant pas de données sensibles.
