using ExileCore2.PoEMemory;
using ExileCore2;
using System.Collections.Generic;
using System.Linq;

namespace WheresMyReforgeAt
{
    public class ReforgeUIHandler
    {
        private const string ReforgeBackgroundTexture = "Art/Textures/Interface/2D/2DArt/UIImages/Common/Background1.dds";
        private readonly GameController _gameController;

        public ReforgeUIHandler(GameController gameController)
        {
            _gameController = gameController;
        }

        public ReforgeUIElements FindReforgeElements()
        {
            var reforgePanel = FindReforgePanel();
            if (reforgePanel == null) return null;

            var reforgeButton = FindReforgeButton(reforgePanel);
            if (reforgeButton == null) return null;

            var mapSlots = CreateMapSlots(reforgePanel).ToList();
            if (mapSlots.Any(slot => slot == null))
            {
                DebugWindow.LogError("Failed to find all map slots");
                return null;
            }

            return new ReforgeUIElements(reforgePanel, reforgeButton, mapSlots);
        }

        private Element FindReforgePanel()
        {
            var panels = _gameController.IngameState.IngameUi.Children
                .Where(x => x.TextureName == ReforgeBackgroundTexture
                           && x.IsValid
                           && x.IsVisible
                           && x.ChildCount == 4)
                .ToList();

            if (panels.Count != 1)
            {
                DebugWindow.LogError($"Expected 1 reforge panel, found {panels.Count}");
                return null;
            }

            return panels[0];
        }

        private Element FindReforgeButton(Element reforgePanel)
        {
            var button = reforgePanel.GetChildFromIndices(3, 1, 0, 0);
            if (button == null)
            {
                DebugWindow.LogError("Failed to find reforge button");
                return null;
            }
            return button;
        }

        private static IEnumerable<Element> CreateMapSlots(Element reforgePanel)
        {
            for (int i = 1; i <= 4; i++)
            {
                yield return reforgePanel.GetChildFromIndices(3, 1, i);
            }
        }
    }
}
