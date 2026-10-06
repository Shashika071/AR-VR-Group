using UnityEngine;

namespace ReefExplorer.Survey
{
    [CreateAssetMenu(menuName = "Reef Explorer/Species Definition", fileName = "Species_")]
    public sealed class SpeciesDefinition : ScriptableObject
    {
        [SerializeField] string speciesId = "species_id";
        [SerializeField] string displayName = "Species";
        [TextArea] [SerializeField] string description = "Educational species description.";
        [SerializeField] Color accentColor = new Color(0.2f, 0.75f, 0.9f);

        public string SpeciesId => speciesId;
        public string DisplayName => displayName;
        public string Description => description;
        public Color AccentColor => accentColor;
    }
}
