# MyBreadboard

**Dessinez vos idées de circuits électriques simplement.** MyBreadboard est une application Windows légère pour composer des schémas visuels, annoter des éléments et représenter des schémas électriques simples.

## État du projet et fonctionnalités

MyBreadboard est un prototype fonctionnel destiné au dessin de schémas électriques simples. L’espace de dessin fait 3000 × 2000 unités et l’interface fournit une palette d’éléments.

### Fonctionnalités disponibles

- Ajouter des symboles : les symboles actuellement supportés sont : générateurs de tension et de courant, résistance, interrupteur, diode, LED, bouton poussoir et annotation.
- Sélectionner les objets, les déplacer sur la feuille et supprimer l’objet sélectionné via le menu contextuel ou la touche **Suppr**.
- Faire pivoter le composant sélectionné de 90° avec **Espace**.
- Créer des fils horizontaux ou verticaux avec le bouton Fil ou la touche **W** ; quitter le mode fil avec **Échap** ou **W**.
- Ajouter et modifier le texte d’une annotation par double-clic. Les annotations peuvent aussi être déplacées et supprimées comme les autres objets.
- Créer un nouveau schéma, ouvrir et enregistrer des documents au format `.mybrd`. Les fichiers enregistrent les composants, leurs positions, dimensions et rotations, le texte des annotations ainsi que les tracés de fils.
- Utiliser **Ctrl+N**, **Ctrl+O** et **Ctrl+S** respectivement pour Nouveau, Ouvrir et Enregistrer.

### Limitations connues

- **Copier, coller et Tout sélectionner** sont présents, mais ne sont pas encore fiables à 100 % ; leur comportement peut être incomplet ou inattendu.
- **Le zoom n’est pas implémenté bien que présent dans le menu du haut.**
- Certaines interactions peuvent nécessiter des améliorations.
- De nombreux composants simples manquent.
- Certains symboles, en particulier ceux du bouton poussoir et de la LED, sont à revoir.

### Non-implémentations
MyBreadboard est un outil de représentation visuelle de circuits électriques. Il n'est pas conçu pour réaliser des simulations.\
De même, les composants jugés trop complexes seront refusés.

## Prérequis

- Windows 10 ou Windows 11 (WPF ne fonctionne que sur Windows).
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Visual Studio 2022 avec la charge de travail **Développement .NET Desktop** (facultatif si vous utilisez la ligne de commande).

## Compiler et lancer

Depuis la racine du dépôt, dans PowerShell ou un terminal Windows :

```powershell
dotnet restore .\MyBreadboard.sln
dotnet build .\MyBreadboard.sln
dotnet run --project .\src\MyBreadboard.App\MyBreadboard.App.csproj
```

Pour ouvrir le projet dans Visual Studio, ouvrez `MyBreadboard.sln`, puis lancez-le avec **F5**.

## Raccourcis clavier

| Raccourci | Action |
| --- | --- |
| `Ctrl+N` | Nouveau schéma |
| `Ctrl+O` | Ouvrir un schéma |
| `Ctrl+S` | Enregistrer le schéma |
| `W` | Activer ou désactiver le mode fil |
| `Échap` | Quitter le mode fil |
| `Espace` | Faire pivoter l’objet sélectionné de 90° |
| `Suppr` | Supprimer l’objet sélectionné |

## Structure du dépôt

```text
MyBreadboard/
├── MyBreadboard.sln
├── README.md
├── CONTRIBUTING.md
└── src/
    └── MyBreadboard.App/
        ├── App.xaml
        ├── App.xaml.cs
        ├── MyBreadboard.App.csproj
        ├── Models/
        ├── Properties/
        └── Views/
```

Consultez [`CONTRIBUTING.md`](CONTRIBUTING.md) pour les instructions de configuration locale, de compilation et de contribution.

## Avertissement et clause de non-responsabilité

MyBreadboard est un outil de dessin destiné à représenter visuellement des idées de circuits. Il ne vérifie pas l’exactitude électrique d’un schéma, ne calcule pas les tensions ou courants, ne simule pas le comportement d’un circuit et ne garantit pas la sécurité d’une conception.

**N’utilisez pas les dessins produits par MyBreadboard comme seule base pour construire, installer ou réparer un circuit électrique.** Toute réalisation physique doit être conçue et vérifiée par une personne qualifiée, conformément aux normes et règles de sécurité applicables. L’électricité peut causer des blessures graves, des dommages matériels ou la mort.

Le logiciel est fourni « en l’état », sans garantie d’exactitude, de disponibilité ou d’adéquation à un usage particulier. Les auteurs et contributeurs ne sauraient être tenus responsables des dommages résultant de son utilisation ou de l’interprétation des schémas créés avec l’application.
