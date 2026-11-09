using RFactAI.Application.Facts;

namespace RFactAI.Infrastructure;

public sealed class StaticFallbackFactProvider : IFallbackFactProvider
{
    private static readonly Dictionary<string, string[]> Facts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["science"] =
        [
            "Water expands by about 9% when it freezes into ice.",
            "A day on Venus is longer than a year on Venus.",
            "Bananas are slightly radioactive because they contain potassium-40.",
            "Light from the Sun takes about 8 minutes and 20 seconds to reach Earth.",
            "Honey never spoils; edible honey has been found in ancient Egyptian tombs."
        ],
        ["space"] =
        [
            "A day on Venus is longer than its year.",
            "Neutron stars can spin hundreds of times per second.",
            "There are more stars in the observable universe than grains of sand on Earth's beaches.",
            "Footprints on the Moon can last millions of years because there is no wind or water to erode them.",
            "Jupiter's Great Red Spot is a storm that has been observed for at least 150 years."
        ],
        ["history"] =
        [
            "Cleopatra lived closer in time to the Moon landing than to the building of the Great Pyramid of Giza.",
            "Oxford University is older than the Aztec Empire.",
            "The shortest war in history, between Britain and Zanzibar in 1896, lasted under 45 minutes.",
            "The Great Wall of China was built over many centuries by several dynasties.",
            "The printing press was introduced to Europe by Johannes Gutenberg around 1440."
        ],
        ["animals"] =
        [
            "Octopuses have three hearts and blue blood.",
            "A group of flamingos is called a flamboyance.",
            "Wombat droppings are cube-shaped.",
            "Crows can recognise individual human faces.",
            "A blue whale's heart can weigh as much as an automobile."
        ],
        ["geography"] =
        [
            "Russia spans eleven time zones.",
            "Canada has more lakes than the rest of the world's countries combined.",
            "The Dead Sea shore is the lowest land elevation on Earth.",
            "Africa is the only continent that lies in all four hemispheres.",
            "Mount Everest grows by a few millimetres every year due to tectonic movement."
        ]
    };

    public FactResponse? Get(string category, int count)
    {
        if (string.IsNullOrWhiteSpace(category) || !Facts.TryGetValue(category.Trim(), out var pool))
            return null;

        var picked = pool.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        return new FactResponse(category.Trim(), picked);
    }
}
