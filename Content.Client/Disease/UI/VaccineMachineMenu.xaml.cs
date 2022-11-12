using Content.Shared.Materials;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Disease.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class VaccineMachineMenu : DefaultWindow
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly SharedMaterialStorageSystem _storage;

        public VaccineMachineBoundUserInterface Owner { get; }

        public event Action<BaseButton.ButtonEventArgs>? OnServerSelectionButtonPressed;

        private List<(string id, string name)> _knownDiseasePrototypes = new();
        public (string id, string name) DiseaseSelected;
        public bool Enough = false;
        public bool Locked = false;
        public int Cost => CreateAmount.Value * 4;

        public VaccineMachineMenu(VaccineMachineBoundUserInterface owner)
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            _storage = _entityManager.EntitySysManager.GetEntitySystem<SharedMaterialStorageSystem>();

            Owner = owner;

            ServerSelectionButton.OnPressed += a => OnServerSelectionButtonPressed?.Invoke(a);
            DiseaseSelected = ("", ""); // nullability was a bit sussy so

            KnownDiseases.OnItemSelected += KnownDiseaseSelected;
            CreateAmount.ValueChanged += HandleAmountChanged;

            CreateButton.OnPressed += _ =>
            {
                CreateVaccine();
            };
            ServerSyncButton.OnPressed += _ =>
            {
                RequestSync();
            };
        }

        /// <summary>
        ///     Called when a known disease is selected.
        /// </summary>
        private void KnownDiseaseSelected(ItemList.ItemListSelectedEventArgs obj)
        {
            DiseaseSelected = _knownDiseasePrototypes[obj.ItemIndex];
            CreateButton.Disabled = !Enough || Locked;

            PopulateSelectedDisease();
        }

        /// <summary>
        ///     Sends a message to create a vaccine of the selected disease.
        /// </summary>
        private void CreateVaccine()
        {
            if (DiseaseSelected == ("", ""))
                return;

            if (CreateAmount.Value <= 0)
                return;

            Owner.CreateVaccineMessage(DiseaseSelected.id, CreateAmount.Value);
        }

        private void RequestSync()
        {
            Owner.RequestSync();
        }

        public void PopulateDiseases(List<(string id, string name)> diseases)
        {
            KnownDiseases.Clear();

            _knownDiseasePrototypes.Clear();

            foreach (var disease in diseases)
            {
                KnownDiseases.AddItem(disease.name);
                _knownDiseasePrototypes.Add((disease.id, disease.name));
            }
        }

        public void UpdateCost()
        {
            BiomassCost.Text = Loc.GetString("vaccine-machine-menu-biomass-cost", ("value", Cost));
        }

        public void UpdateLocked(bool locked)
        {
            Locked = locked;
        }

        private void HandleAmountChanged(object? sender, ValueChangedEventArgs args)
        {
            UpdateCost();
        }

        public void PopulateSelectedDisease()
        {
            if (DiseaseSelected == ("", ""))
            {
                CreateButton.Disabled = true;
                DiseaseName.Text = Loc.GetString("vaccine-machine-menu-none-selected");
                return;
            }

            DiseaseName.Text = DiseaseSelected.name;
        }

        public void PopulateBiomass(EntityUid machine)
        {
            var amt = _storage.GetMaterialAmount(machine, "Biomass");
            Enough = (amt > Cost);
            BiomassCurrent.Text = Loc.GetString("vaccine-machine-menu-biomass-current", ("value", amt));

            if (DiseaseSelected != ("", ""))
                CreateButton.Disabled = !Enough || Locked;
        }
    }
}
