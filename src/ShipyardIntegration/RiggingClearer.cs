// Clears removable rigging in dependency order through the pending native shipyard order.
using System;
using System.Linq;

namespace SaveShipConfiguration.ShipyardIntegration
{
    internal static class RiggingClearer
    {
        internal static string Clear(Shipyard shipyard)
        {
            var boat = shipyard.GetCurrentBoat();
            var parts = boat.GetComponent<BoatCustomParts>();
            var order = shipyard.partsInstaller.GetCurrentOrder();
            int retained = 0;

            shipyard.sailInstaller.StopAllCoroutines();
            shipyard.sailInstaller.SelectMast(mast: null);
            foreach (var mast in boat.GetComponent<BoatRefs>().masts)
            {
                if (!mast)
                    continue;
                foreach (var obj in mast.sails.ToArray())
                    shipyard.sailInstaller.DestroySail(mast: mast, sail: obj.GetComponent<Sail>());
            }
            shipyard.UpdateOrder();

            // Complete each group before touching the next: stays, shrouds, masts, bowsprits.
            for (int group = 0; group < 4; group++)
            {
                for (int index = 0; index < parts.availableParts.Count; index++)
                {
                    var part = parts.availableParts[index];
                    if (
                        Group(name: part.partOptions[order.orderedOptions[index]].optionName)
                        != group
                    )
                        continue;
                    int empty = part.partOptions.FindIndex(option =>
                        option.optionName.StartsWith("(no ", StringComparison.OrdinalIgnoreCase)
                    );
                    // Some native masts, stays, and shrouds have no removal option.
                    if (empty < 0)
                    {
                        retained++;
                        continue;
                    }
                    order.orderedOptions[index] = empty;
                }
                shipyard.UpdateOrder();
            }

            ShipyardUI.instance.ResetPartsErrorTexts();
            ShipyardUI.instance.ChangeMenuCategory(newCategory: 0);
            ShipyardUI.instance.UpdateSailsCountText();
            shipyard.UpdateOrder();
            ShipyardUI.instance.RefreshButtons();
            return "Cleared removable rigging. "
                + (retained > 0 ? retained + " required parts have no removal option. " : "")
                + "Review and confirm the shipyard order, or cancel normally.";
        }

        internal static int Group(string name)
        {
            name = name.ToLowerInvariant();
            // Shroud and stay names can also contain 'mast'; test them first.
            if (name.Contains("stay"))
                return 0;
            if (name.Contains("shroud") || name.Contains("shrowd"))
                return 1;
            if (name.Contains("bowsprit") || name.Contains("bowsprint"))
                return 3;
            if (name.Contains("mast"))
                return 2;
            return -1;
        }
    }
}
