using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Elympics.Editor.Replay
{
    internal class TickListDisplayer
    {
        private static readonly int TickEntryHeight = 18;

        private float _expectedTime;

        internal List<TickEntryData> AllTicksData { get; }
        internal ListView TickListElement { get; }

        internal TickListDisplayer(VisualElement root, VisualTreeAsset tickEntryTemplate, TickDataDisplayer tickDataDisplayer)
        {
            // Create data collection to bind
            AllTicksData = new List<TickEntryData>();

            // Store a reference to the list element and initialize it
            TickListElement = root.Q<ListView>("tick-list");
            InitializeTickList(tickEntryTemplate);

            // Register tick selection callback
            TickListElement.onSelectionChange += selection => tickDataDisplayer.SetData(selection.FirstOrDefault() as TickEntryData, _expectedTime);
        }

        private void InitializeTickList(VisualTreeAsset listEntryTemplate)
        {
            // Ensure proper displaying
            TickListElement.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            // Set up a make item function for a list entry
            TickListElement.makeItem = () =>
            {
                // Instantiate the UXML template for the entry and provide corresponding style
                var newListEntry = listEntryTemplate.CloneTree();
                newListEntry.AddToClassList("tick-entry");

                // Instantiate a controller for the data
                var newListEntryLogic = new TickEntryDisplayer();

                // Assign the controller script to the visual element
                newListEntry.userData = newListEntryLogic;

                // Initialize the controller script
                newListEntryLogic.InitVisualElement(newListEntry);

                // Return the root of the instantiated visual tree
                return newListEntry;
            };

            // Set up bind function for a specific list entry
            TickListElement.bindItem = (item, index) => (item.userData as TickEntryDisplayer)?.SetTickEntryData(AllTicksData[index], _expectedTime);

            // Set the actual item's source list/array
            TickListElement.itemsSource = AllTicksData;

            // Set up list properties
            TickListElement.fixedItemHeight = TickEntryHeight;
            TickListElement.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        }

        internal void SelectTick(long tick) => TickListElement.SetSelection(AllTicksData.FindIndex(x => x.Tick == tick));

        internal void SetSnapshots(IEnumerable<ElympicsSnapshotWithMetadata> snapshots, int numberOfPlayers, float expectedTime)
        {
            AllTicksData.Clear();
            _expectedTime = expectedTime;
            AllTicksData.AddRange(snapshots.Select(snapshot => new TickEntryData(snapshot, numberOfPlayers)));
        }
    }
}
