using ClosedXML.Excel;
using PeopleRise.Modules.JobReward.Application.Methodologies.Versions;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.ImportExport;

// Reads/writes the 5-sheet methodology workbook (Version, Factors, Questions, AnswerOptions, GradeMappings).
// Sheets are joined by workbook-local row keys (Key/FactorKey/QuestionKey), since Question has no
// natural code. Grades themselves aren't part of the file - GradeCode must already exist in the tenant.
internal static class MethodologyWorkbook
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string VersionSheet = "Version";
    private const string FactorsSheet = "Factors";
    private const string QuestionsSheet = "Questions";
    private const string AnswerOptionsSheet = "AnswerOptions";
    private const string GradeMappingsSheet = "GradeMappings";

    public static byte[] Build(MethodologyVersionDetailDto version)
    {
        using var workbook = new XLWorkbook();

        var versionInfo = workbook.Worksheets.Add(VersionSheet);
        WriteHeader(versionInfo, "MinPoints", "MaxPoints");
        versionInfo.Cell(2, 1).Value = version.MinPoints;
        versionInfo.Cell(2, 2).Value = version.MaxPoints;

        var factors = workbook.Worksheets.Add(FactorsSheet);
        WriteHeader(factors, "Key", "Code", "NameEn", "NameAr", "Weight", "SortOrder");

        var questions = workbook.Worksheets.Add(QuestionsSheet);
        WriteHeader(questions, "Key", "FactorKey", "QuestionTextEn", "QuestionTextAr", "HelpTextEn", "HelpTextAr", "QuestionType", "Weight", "IsRequired", "SortOrder");

        var answerOptions = workbook.Worksheets.Add(AnswerOptionsSheet);
        WriteHeader(answerOptions, "QuestionKey", "LabelEn", "LabelAr", "Rating", "SortOrder");

        var gradeMappings = workbook.Worksheets.Add(GradeMappingsSheet);
        WriteHeader(gradeMappings, "GradeCode", "MinScore", "MaxScore");

        var factorRow = 2;
        var questionRow = 2;
        var optionRow = 2;
        var nextFactorKey = 1;
        var nextQuestionKey = 1;

        foreach (var factor in version.Factors)
        {
            var factorKey = nextFactorKey++;

            factors.Cell(factorRow, 1).Value = factorKey;
            factors.Cell(factorRow, 2).Value = factor.Code;
            factors.Cell(factorRow, 3).Value = factor.NameEn;
            factors.Cell(factorRow, 4).Value = factor.NameAr;
            factors.Cell(factorRow, 5).Value = factor.Weight;
            factors.Cell(factorRow, 6).Value = factor.SortOrder;
            factorRow++;

            foreach (var question in factor.Questions)
            {
                var questionKey = nextQuestionKey++;

                questions.Cell(questionRow, 1).Value = questionKey;
                questions.Cell(questionRow, 2).Value = factorKey;
                questions.Cell(questionRow, 3).Value = question.QuestionTextEn;
                questions.Cell(questionRow, 4).Value = question.QuestionTextAr;
                questions.Cell(questionRow, 5).Value = question.HelpTextEn;
                questions.Cell(questionRow, 6).Value = question.HelpTextAr;
                questions.Cell(questionRow, 7).Value = question.QuestionType;
                questions.Cell(questionRow, 8).Value = question.Weight;
                questions.Cell(questionRow, 9).Value = question.IsRequired;
                questions.Cell(questionRow, 10).Value = question.SortOrder;
                questionRow++;

                foreach (var option in question.Options)
                {
                    answerOptions.Cell(optionRow, 1).Value = questionKey;
                    answerOptions.Cell(optionRow, 2).Value = option.LabelEn;
                    answerOptions.Cell(optionRow, 3).Value = option.LabelAr;
                    answerOptions.Cell(optionRow, 4).Value = option.Rating;
                    answerOptions.Cell(optionRow, 5).Value = option.SortOrder;
                    optionRow++;
                }
            }
        }

        var mappingRow = 2;
        foreach (var mapping in version.GradeMappings)
        {
            gradeMappings.Cell(mappingRow, 1).Value = mapping.GradeCode;
            gradeMappings.Cell(mappingRow, 2).Value = mapping.MinScore;
            gradeMappings.Cell(mappingRow, 3).Value = mapping.MaxScore;
            mappingRow++;
        }

        foreach (var sheet in workbook.Worksheets)
        {
            sheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static Result<ParsedMethodologyWorkbook> Parse(byte[] content)
    {
        var errors = new List<string>();

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        var versionInfo = ReadVersionInfo(workbook, errors);
        var factors = ReadFactors(workbook, errors);
        var questions = ReadQuestions(workbook, errors);
        var answerOptions = ReadAnswerOptions(workbook, errors);
        var gradeMappings = ReadGradeMappings(workbook, errors);

        var factorKeys = factors.Select(f => f.Key).ToHashSet();
        foreach (var duplicate in factors.Select(f => f.Key).GroupBy(k => k).Where(g => g.Count() > 1))
        {
            errors.Add($"Factors: Key {duplicate.Key} is used more than once.");
        }

        foreach (var question in questions.Where(q => !factorKeys.Contains(q.FactorKey)))
        {
            errors.Add($"Questions: row references unknown FactorKey {question.FactorKey}.");
        }

        var questionKeys = questions.Select(q => q.Key).ToHashSet();
        foreach (var duplicate in questions.Select(q => q.Key).GroupBy(k => k).Where(g => g.Count() > 1))
        {
            errors.Add($"Questions: Key {duplicate.Key} is used more than once.");
        }

        foreach (var option in answerOptions.Where(o => !questionKeys.Contains(o.QuestionKey)))
        {
            errors.Add($"AnswerOptions: row references unknown QuestionKey {option.QuestionKey}.");
        }

        if (errors.Count > 0)
        {
            return Error.Validation(string.Join(" | ", errors));
        }

        return new ParsedMethodologyWorkbook(versionInfo, factors, questions, answerOptions, gradeMappings);
    }

    private static void WriteHeader(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        sheet.Row(1).Style.Font.Bold = true;
    }

    private static IXLWorksheet? RequireSheet(XLWorkbook workbook, string name, List<string> errors)
    {
        if (workbook.Worksheets.TryGetWorksheet(name, out var sheet))
        {
            return sheet;
        }

        errors.Add($"Missing required sheet '{name}'.");
        return null;
    }

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ParsedVersionInfoRow ReadVersionInfo(XLWorkbook workbook, List<string> errors)
    {
        var sheet = RequireSheet(workbook, VersionSheet, errors);
        if (sheet is null) return new ParsedVersionInfoRow(200, 1000);

        var row = sheet.RowsUsed().Skip(1).FirstOrDefault();
        if (row is null)
        {
            errors.Add($"{VersionSheet}: a MinPoints/MaxPoints row is required.");
            return new ParsedVersionInfoRow(200, 1000);
        }

        if (!row.Cell(1).TryGetValue(out int minPoints) || !row.Cell(2).TryGetValue(out int maxPoints))
        {
            errors.Add($"{VersionSheet}: MinPoints and MaxPoints must be whole numbers.");
            return new ParsedVersionInfoRow(200, 1000);
        }

        return new ParsedVersionInfoRow(minPoints, maxPoints);
    }

    private static List<ParsedFactorRow> ReadFactors(XLWorkbook workbook, List<string> errors)
    {
        var rows = new List<ParsedFactorRow>();
        var sheet = RequireSheet(workbook, FactorsSheet, errors);
        if (sheet is null) return rows;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var rowNo = row.RowNumber();

            if (!row.Cell(1).TryGetValue(out int key))
            {
                errors.Add($"{FactorsSheet} row {rowNo}: Key must be a whole number.");
                continue;
            }

            var code = row.Cell(2).GetString().Trim();
            var nameEn = row.Cell(3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(nameEn))
            {
                errors.Add($"{FactorsSheet} row {rowNo}: NameEn is required.");
                continue;
            }

            var nameAr = NullIfBlank(row.Cell(4).GetString());

            if (!row.Cell(5).TryGetValue(out decimal weight))
            {
                errors.Add($"{FactorsSheet} row {rowNo}: Weight must be a number.");
                continue;
            }

            var sortOrder = row.Cell(6).IsEmpty() ? 0 : row.Cell(6).GetValue<int>();

            rows.Add(new ParsedFactorRow(key, code, nameEn, nameAr, weight, sortOrder));
        }

        return rows;
    }

    private static List<ParsedQuestionRow> ReadQuestions(XLWorkbook workbook, List<string> errors)
    {
        var rows = new List<ParsedQuestionRow>();
        var sheet = RequireSheet(workbook, QuestionsSheet, errors);
        if (sheet is null) return rows;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var rowNo = row.RowNumber();

            if (!row.Cell(1).TryGetValue(out int key))
            {
                errors.Add($"{QuestionsSheet} row {rowNo}: Key must be a whole number.");
                continue;
            }

            if (!row.Cell(2).TryGetValue(out int factorKey))
            {
                errors.Add($"{QuestionsSheet} row {rowNo}: FactorKey must be a whole number.");
                continue;
            }

            var questionTextEn = row.Cell(3).GetString().Trim();

            if (string.IsNullOrWhiteSpace(questionTextEn))
            {
                errors.Add($"{QuestionsSheet} row {rowNo}: QuestionTextEn is required.");
                continue;
            }

            var questionType = row.Cell(7).GetString().Trim();

            if (!row.Cell(8).TryGetValue(out decimal weight))
            {
                errors.Add($"{QuestionsSheet} row {rowNo}: Weight must be a number.");
                continue;
            }

            var isRequired = row.Cell(9).IsEmpty() || row.Cell(9).GetValue<bool>();

            rows.Add(new ParsedQuestionRow(
                key,
                factorKey,
                questionTextEn,
                NullIfBlank(row.Cell(4).GetString()),
                NullIfBlank(row.Cell(5).GetString()),
                NullIfBlank(row.Cell(6).GetString()),
                questionType,
                weight,
                isRequired,
                row.Cell(10).IsEmpty() ? 0 : row.Cell(10).GetValue<int>()));
        }

        return rows;
    }

    private static List<ParsedAnswerOptionRow> ReadAnswerOptions(XLWorkbook workbook, List<string> errors)
    {
        var rows = new List<ParsedAnswerOptionRow>();
        var sheet = RequireSheet(workbook, AnswerOptionsSheet, errors);
        if (sheet is null) return rows;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var rowNo = row.RowNumber();

            if (!row.Cell(1).TryGetValue(out int questionKey))
            {
                errors.Add($"{AnswerOptionsSheet} row {rowNo}: QuestionKey must be a whole number.");
                continue;
            }

            var labelEn = row.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(labelEn))
            {
                errors.Add($"{AnswerOptionsSheet} row {rowNo}: LabelEn is required.");
                continue;
            }

            if (!row.Cell(4).TryGetValue(out int rating))
            {
                errors.Add($"{AnswerOptionsSheet} row {rowNo}: Rating must be a whole number.");
                continue;
            }

            rows.Add(new ParsedAnswerOptionRow(
                questionKey,
                labelEn,
                NullIfBlank(row.Cell(3).GetString()),
                rating,
                row.Cell(5).IsEmpty() ? 0 : row.Cell(5).GetValue<int>()));
        }

        return rows;
    }

    private static List<ParsedGradeMappingRow> ReadGradeMappings(XLWorkbook workbook, List<string> errors)
    {
        var rows = new List<ParsedGradeMappingRow>();
        var sheet = RequireSheet(workbook, GradeMappingsSheet, errors);
        if (sheet is null) return rows;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var rowNo = row.RowNumber();

            var gradeCode = row.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(gradeCode))
            {
                errors.Add($"{GradeMappingsSheet} row {rowNo}: GradeCode is required.");
                continue;
            }

            int? minScore = null;
            int? maxScore = null;

            if (!row.Cell(2).IsEmpty())
            {
                if (!row.Cell(2).TryGetValue(out int min))
                {
                    errors.Add($"{GradeMappingsSheet} row {rowNo}: MinScore must be a whole number.");
                    continue;
                }
                minScore = min;
            }

            if (!row.Cell(3).IsEmpty())
            {
                if (!row.Cell(3).TryGetValue(out int max))
                {
                    errors.Add($"{GradeMappingsSheet} row {rowNo}: MaxScore must be a whole number.");
                    continue;
                }
                maxScore = max;
            }

            if (minScore is not null && maxScore is not null && maxScore < minScore)
            {
                errors.Add($"{GradeMappingsSheet} row {rowNo}: MaxScore must be >= MinScore.");
                continue;
            }

            rows.Add(new ParsedGradeMappingRow(gradeCode, minScore, maxScore));
        }

        return rows;
    }
}
