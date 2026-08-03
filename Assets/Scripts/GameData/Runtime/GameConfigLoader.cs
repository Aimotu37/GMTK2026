using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class GameConfigLoader
{
    private const int SupportedSchemaVersion = 1;

    private static readonly string[] ConfigAddresses =
    {
        GameConfig.MetaAddress,
        GameConfig.CasesAddress,
        GameConfig.ItemsAddress,
        GameConfig.OptionsAddress,
        GameConfig.DialoguesAddress,
        GameConfig.DemonLinesAddress,
        GameConfig.DebuffsAddress
    };

    public GameConfigSnapshot Snapshot { get; private set; }
    public bool IsLoading { get; private set; }
    public bool IsLoaded => Snapshot != null;

    public IEnumerator Load()
    {
        if (IsLoading)
        {
            throw new InvalidOperationException("Game config is already loading.");
        }

        IsLoading = true;
        Snapshot = null;

        try
        {
            var csvTexts = new Dictionary<string, string>(ConfigAddresses.Length);

            foreach (var address in ConfigAddresses)
            {
                AsyncOperationHandle<TextAsset> handle =
                    Addressables.LoadAssetAsync<TextAsset>(address);

                try
                {
                    yield return handle;

                    if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    {
                        string reason = handle.OperationException?.Message ?? "Unknown error.";
                        throw new InvalidOperationException(
                            $"Failed to load game config '{address}': {reason}");
                    }

                    csvTexts.Add(address, handle.Result.text);
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            Snapshot = BuildSnapshot(csvTexts);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static GameConfigSnapshot BuildSnapshot(
        IReadOnlyDictionary<string, string> csvTexts)
    {
        MetaConfig meta = ParseMeta(csvTexts[GameConfig.MetaAddress]);
        Dictionary<int, DialogueConfig> dialogues =
            ParseDialogues(csvTexts[GameConfig.DialoguesAddress]);
        Dictionary<int, CaseConfig> cases =
            ParseCases(csvTexts[GameConfig.CasesAddress], dialogues);
        Dictionary<int, ItemsConfig> items =
            ParseItems(csvTexts[GameConfig.ItemsAddress]);
        Dictionary<int, OptionsConfig> options =
            ParseOptions(csvTexts[GameConfig.OptionsAddress]);
        Dictionary<int, DemonLinesConfig> demonLines =
            ParseDemonLines(csvTexts[GameConfig.DemonLinesAddress]);
        Dictionary<int, DebuffConfig> debuffs =
            ParseDebuffs(csvTexts[GameConfig.DebuffsAddress]);

        Validate(meta, cases, items, options, dialogues, demonLines);

        return new GameConfigSnapshot(
            meta,
            cases,
            items,
            options,
            dialogues,
            demonLines,
            debuffs);
    }

    private static MetaConfig ParseMeta(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows =
            CsvParser.Parse(csvText, "config_meta.csv");

        if (rows.Count != 1)
        {
            throw new FormatException(
                $"config_meta.csv must contain exactly one data row, but found {rows.Count}.");
        }

        CsvParser.Row row = rows[0];
        return new MetaConfig(
            row.GetInt32("schema_version"),
            row.GetString("content_version"),
            row.GetInt32("default_case_id"));
    }

    private static Dictionary<int, DialogueConfig> ParseDialogues(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows =
            CsvParser.Parse(csvText, "dialogues.csv");
        var result = new Dictionary<int, DialogueConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int dialogueId = row.GetInt32("dialogue_id");
            bool showPortrait = row.GetBoolean("show_portrait");
            string portraitAddress = row.GetString("portrait_address", allowEmpty: true);

            if (showPortrait && portraitAddress.Length == 0)
            {
                throw RowError(row, "portrait_address", "Value cannot be empty when show_portrait is true.");
            }

            var config = new DialogueConfig(
                dialogueId,
                row.GetEnum<DialogueType>("dialogue_type"),
                row.GetInt32("owner_id"),
                row.GetInt32("sequence"),
                row.GetString("text_key"),
                showPortrait,
                portraitAddress);

            AddUnique(result, dialogueId, config, row, "dialogue_id");
        }

        return result;
    }

    private static Dictionary<int, CaseConfig> ParseCases(
        string csvText,
        IReadOnlyDictionary<int, DialogueConfig> dialogues)
    {
        IReadOnlyList<CsvParser.Row> rows = CsvParser.Parse(csvText, "cases.csv");
        var result = new Dictionary<int, CaseConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int caseId = row.GetInt32("case_id");
            IReadOnlyList<DialogueConfig> storyLines =
                GetCaseDialogues(dialogues, caseId, DialogueType.CaseStory);
            IReadOnlyList<DialogueConfig> truthLines =
                GetCaseDialogues(dialogues, caseId, DialogueType.CaseTruth);

            var config = new CaseConfig(
                caseId,
                row.GetInt32("sort_order"),
                row.GetString("scene_name"),
                row.GetString("name_key"),
                row.GetString("scene_label_key"),
                row.GetInt32("initial_words"),
                storyLines,
                truthLines);

            AddUnique(result, caseId, config, row, "case_id");
        }

        return result;
    }

    private static Dictionary<int, ItemsConfig> ParseItems(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows = CsvParser.Parse(csvText, "items.csv");
        var result = new Dictionary<int, ItemsConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int itemId = row.GetInt32("item_id");
            var config = new ItemsConfig(
                itemId,
                row.GetInt32("case_id"),
                row.GetInt32("display_order"),
                row.GetString("name_key"),
                row.GetString("clue_key"));

            AddUnique(result, itemId, config, row, "item_id");
        }

        return result;
    }

    private static Dictionary<int, OptionsConfig> ParseOptions(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows = CsvParser.Parse(csvText, "options.csv");
        var result = new Dictionary<int, OptionsConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int optionId = row.GetInt32("option_id");
            var config = new OptionsConfig(
                optionId,
                row.GetInt32("case_id"),
                row.GetInt32("display_order"),
                row.GetString("text_key"),
                row.GetBoolean("is_correct"));

            AddUnique(result, optionId, config, row, "option_id");
        }

        return result;
    }

    private static Dictionary<int, DemonLinesConfig> ParseDemonLines(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows =
            CsvParser.Parse(csvText, "demon_lines.csv");
        var result = new Dictionary<int, DemonLinesConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int demonId = row.GetInt32("demon_id");
            var config = new DemonLinesConfig(
                demonId,
                row.GetString("default_key"),
                row.GetString("hurt_key"),
                row.GetString("mock_key"),
                row.GetInt32("defeat_dialogue_id"),
                row.GetSingle("duration_seconds"));

            AddUnique(result, demonId, config, row, "demon_id");
        }

        return result;
    }

    private static Dictionary<int, DebuffConfig> ParseDebuffs(string csvText)
    {
        IReadOnlyList<CsvParser.Row> rows = CsvParser.Parse(csvText, "debuffs.csv");
        var result = new Dictionary<int, DebuffConfig>(rows.Count);

        foreach (CsvParser.Row row in rows)
        {
            int debuffId = row.GetInt32("debuff_id");
            var config = new DebuffConfig(
                debuffId,
                row.GetString("name_key"),
                row.GetString("description_key"),
                row.GetEnum<DebuffType>("effect_type"),
                row.GetSingle("effect_param"));

            AddUnique(result, debuffId, config, row, "debuff_id");
        }

        return result;
    }

    private static IReadOnlyList<DialogueConfig> GetCaseDialogues(
        IReadOnlyDictionary<int, DialogueConfig> dialogues,
        int caseId,
        DialogueType type)
    {
        var result = new List<DialogueConfig>();

        foreach (DialogueConfig dialogue in dialogues.Values)
        {
            if (dialogue.DialogueType == type && dialogue.OwnerId == caseId)
            {
                result.Add(dialogue);
            }
        }

        result.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
        return result.AsReadOnly();
    }

    private static void Validate(
        MetaConfig meta,
        IReadOnlyDictionary<int, CaseConfig> cases,
        IReadOnlyDictionary<int, ItemsConfig> items,
        IReadOnlyDictionary<int, OptionsConfig> options,
        IReadOnlyDictionary<int, DialogueConfig> dialogues,
        IReadOnlyDictionary<int, DemonLinesConfig> demonLines)
    {
        if (meta.SchemaVersion != SupportedSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported game config schema version {meta.SchemaVersion}. " +
                $"Expected {SupportedSchemaVersion}.");
        }

        if (!cases.ContainsKey(meta.DefaultCaseId))
        {
            throw new InvalidOperationException(
                $"Default case {meta.DefaultCaseId} does not exist.");
        }

        foreach (ItemsConfig item in items.Values)
        {
            if (!cases.ContainsKey(item.CaseId))
            {
                throw new InvalidOperationException(
                    $"Item {item.ItemId} references missing case {item.CaseId}.");
            }
        }

        var correctOptionsByCase = new Dictionary<int, int>();
        foreach (OptionsConfig option in options.Values)
        {
            if (!cases.ContainsKey(option.CaseId))
            {
                throw new InvalidOperationException(
                    $"Option {option.OptionId} references missing case {option.CaseId}.");
            }

            if (option.IsCorrect)
            {
                correctOptionsByCase.TryGetValue(option.CaseId, out int count);
                correctOptionsByCase[option.CaseId] = count + 1;
            }
        }

        foreach (int caseId in cases.Keys)
        {
            correctOptionsByCase.TryGetValue(caseId, out int correctCount);
            if (correctCount != 1)
            {
                throw new InvalidOperationException(
                    $"Case {caseId} must have exactly one correct option, but found {correctCount}.");
            }
        }

        foreach (DialogueConfig dialogue in dialogues.Values)
        {
            if (dialogue.ShowPortrait && string.IsNullOrEmpty(dialogue.PortraitAddress))
            {
                throw new InvalidOperationException(
                    $"Dialogue {dialogue.DialogueId} requires a portrait address.");
            }

            if (dialogue.DialogueType == DialogueType.Opening)
            {
                if (dialogue.OwnerId != 0)
                {
                    throw new InvalidOperationException(
                        $"Opening dialogue {dialogue.DialogueId} must use owner_id 0.");
                }

                continue;
            }

            if (dialogue.DialogueType == DialogueType.CaseStory ||
                dialogue.DialogueType == DialogueType.CaseTruth)
            {
                if (!cases.ContainsKey(dialogue.OwnerId))
                {
                    throw new InvalidOperationException(
                        $"Dialogue {dialogue.DialogueId} references missing case {dialogue.OwnerId}.");
                }

                continue;
            }

            if (dialogue.DialogueType == DialogueType.DemonDefeat &&
                !demonLines.ContainsKey(dialogue.OwnerId))
            {
                throw new InvalidOperationException(
                    $"Dialogue {dialogue.DialogueId} references missing demon {dialogue.OwnerId}.");
            }
        }

        foreach (DemonLinesConfig demon in demonLines.Values)
        {
            if (!dialogues.TryGetValue(demon.DefeatDialogueId, out DialogueConfig defeatDialogue))
            {
                throw new InvalidOperationException(
                    $"Demon {demon.DemonId} references missing defeat dialogue " +
                    $"{demon.DefeatDialogueId}.");
            }

            if (defeatDialogue.DialogueType != DialogueType.DemonDefeat ||
                defeatDialogue.OwnerId != demon.DemonId)
            {
                throw new InvalidOperationException(
                    $"Dialogue {demon.DefeatDialogueId} is not the defeat dialogue for demon " +
                    $"{demon.DemonId}.");
            }
        }
    }

    private static void AddUnique<T>(
        IDictionary<int, T> target,
        int id,
        T value,
        CsvParser.Row row,
        string columnName)
    {
        if (target.ContainsKey(id))
        {
            throw RowError(row, columnName, $"Duplicate ID {id}.");
        }

        target.Add(id, value);
    }

    private static FormatException RowError(
        CsvParser.Row row,
        string columnName,
        string message)
    {
        return new FormatException(
            $"{row.SourceName}:{row.LineNumber} [{columnName}] {message}");
    }
}
