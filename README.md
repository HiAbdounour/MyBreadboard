# MyBreadboard

MyBreadboard est une application Windows de dessin de schémas électriques simples, développée en C# avec WPF et XAML.

## État du projet

Cette première version initialise la solution et affiche une fenêtre WPF maximisée avec une palette, une feuille fixe de 3000 × 2000 unités et un schéma de démonstration. Les interactions d’édition, la sauvegarde et le chargement seront ajoutés dans les prochaines étapes.

Les fils sont des traits visuels : ils ne représentent pas de connexions électriques. Les segments sont horizontaux ou verticaux uniquement.

## Prérequis

- Windows 11 (WPF ne fonctionne que sous Windows)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 avec la charge de travail **Développement .NET Desktop** (facultatif si vous utilisez la ligne de commande)

## Compiler et lancer

Depuis la racine du dépôt, dans PowerShell ou un terminal Windows :

```powershell
dotnet restore .\MyBreadboard.sln
dotnet build .\MyBreadboard.sln
dotnet run --project .\src\MyBreadboard.App\MyBreadboard.App.csproj
```

Pour ouvrir le projet dans Visual Studio, ouvrez `MyBreadboard.sln`, puis lancez-le avec **F5**.


## Structure

```text
MyBreadboard/
├── MyBreadboard.sln
├── README.md
└── src/
    └── MyBreadboard.App/
        ├── App.xaml
        ├── App.xaml.cs
        ├── MyBreadboard.App.csproj
        ├── Models/
        │   ├── Annotation.cs
        │   ├── CircuitComponent.cs
        │   ├── DrawingDocument.cs
        │   └── WireStroke.cs
        ├── Properties/
        │   └── AssemblyInfo.cs
        └── Views/
            ├── MainWindow.xaml
            └── MainWindow.xaml.cs
```
