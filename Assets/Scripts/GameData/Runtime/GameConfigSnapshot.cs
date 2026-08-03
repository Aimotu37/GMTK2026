using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class GameConfig
{
    public const string MetaAddress = "config/csv/meta";
    public const string CasesAddress = "config/csv/cases";
    public const string ItemsAddress = "config/csv/items";
    public const string OptionsAddress = "config/csv/options";
    public const string DialoguesAddress = "config/csv/dialogues";
    public const string DemonLinesAddress = "config/csv/demon_lines";
    public const string DebuffsAddress = "config/csv/debuffs";
}

public sealed class GameConfigSnapshot
{
    public MetaConfig Meta { get; }

    public IReadOnlyDictionary<int, CaseConfig> Cases { get; }
    public IReadOnlyDictionary<int, ItemsConfig> Items { get; }
    public IReadOnlyDictionary<int, OptionsConfig> Options { get; }
    public IReadOnlyDictionary<int, DialogueConfig> Dialogues { get; }
    public IReadOnlyDictionary<int, DemonLinesConfig> DemonLines { get; }
    public IReadOnlyDictionary<int, DebuffConfig> Debuffs { get; }

    public GameConfigSnapshot(
        MetaConfig meta,
        IDictionary<int, CaseConfig> cases,
        IDictionary<int, ItemsConfig> items,
        IDictionary<int, OptionsConfig> options,
        IDictionary<int, DialogueConfig> dialogues,
        IDictionary<int, DemonLinesConfig> demonLines,
        IDictionary<int, DebuffConfig> debuffs)
    {
        Meta = meta ?? throw new ArgumentNullException(nameof(meta));
        Cases = CopyAsReadOnly(cases, nameof(cases));
        Items = CopyAsReadOnly(items, nameof(items));
        Options = CopyAsReadOnly(options, nameof(options));
        Dialogues = CopyAsReadOnly(dialogues, nameof(dialogues));
        DemonLines = CopyAsReadOnly(demonLines, nameof(demonLines));
        Debuffs = CopyAsReadOnly(debuffs, nameof(debuffs));
    }

    private static IReadOnlyDictionary<int, T> CopyAsReadOnly<T>(
        IDictionary<int, T> source,
        string parameterName)
    {
        if (source == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return new ReadOnlyDictionary<int, T>(new Dictionary<int, T>(source));
    }
}
