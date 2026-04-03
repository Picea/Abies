namespace Picea.Abies;

internal static class PatchCanonicalizer
{
    public static IReadOnlyList<Patch> Canonicalize(IReadOnlyList<Patch> patches)
    {
        if (patches.Count < 2)
        {
            return patches;
        }

        var lastMutationIndexByKey = new Dictionary<string, int>(patches.Count);
        var suppressed = new bool[patches.Count];
        var suppressedAny = false;

        for (int i = 0; i < patches.Count; i++)
        {
            if (!TryGetCoalescingKey(patches[i], out var key))
            {
                continue;
            }

            if (lastMutationIndexByKey.TryGetValue(key, out var previousIndex))
            {
                suppressed[previousIndex] = true;
                suppressedAny = true;
            }

            lastMutationIndexByKey[key] = i;
        }

        if (!suppressedAny)
        {
            return patches;
        }

        var canonicalized = new List<Patch>(patches.Count);
        for (int i = 0; i < patches.Count; i++)
        {
            if (!suppressed[i])
            {
                canonicalized.Add(patches[i]);
            }
        }

        return canonicalized;
    }

    private static bool TryGetCoalescingKey(Patch patch, out string key)
    {
        switch (patch)
        {
            case AddAttribute p:
                key = $"attr|{p.Element.Id}|{p.Attribute.Name}";
                return true;

            case UpdateAttribute p:
                key = $"attr|{p.Element.Id}|{p.Attribute.Name}";
                return true;

            case RemoveAttribute p:
                key = $"attr|{p.Element.Id}|{p.Attribute.Name}";
                return true;

            case AddHandler p:
                key = $"handler|{p.Element.Id}|{p.Handler.Name}";
                return true;

            case UpdateHandler p:
                key = $"handler|{p.Element.Id}|{p.OldHandler.Name}";
                return true;

            case RemoveHandler p:
                key = $"handler|{p.Element.Id}|{p.Handler.Name}";
                return true;

            case AddHeadElement p:
                key = $"head|{p.Content.Key}";
                return true;

            case UpdateHeadElement p:
                key = $"head|{p.Content.Key}";
                return true;

            case RemoveHeadElement p:
                key = $"head|{p.Key}";
                return true;

            default:
                key = string.Empty;
                return false;
        }
    }
}
