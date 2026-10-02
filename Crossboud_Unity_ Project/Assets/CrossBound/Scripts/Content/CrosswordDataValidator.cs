using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates crossword JSON before it reaches gameplay.
/// Catches the failure modes that silently break a puzzle:
/// missing fields, words running outside the grid, and — most importantly —
/// crossing conflicts where two words disagree about the letter in a shared cell
/// (which makes the puzzle impossible to complete).
/// </summary>
public static class CrosswordDataValidator
{
    public static List<string> Validate(CrosswordData data)
    {
        List<string> errors = new List<string>();
        if (data == null)
        {
            errors.Add("Crossword data is null.");
            return errors;
        }

        data.NormalizeGeneratedLayout();

        if (data.gridWidth <= 0 || data.gridHeight <= 0)
            errors.Add($"Grid size must be positive (got {data.gridWidth}x{data.gridHeight}).");

        IReadOnlyList<CrosswordQuestion> questions = data.Questions;
        if (questions.Count == 0)
        {
            errors.Add("Crossword has no questions.");
            return errors;
        }

        HashSet<string> seenIds = new HashSet<string>();
        Dictionary<Vector2Int, (string Id, char Letter)> grid = new Dictionary<Vector2Int, (string, char)>();

        foreach (CrosswordQuestion question in questions)
        {
            if (question == null)
            {
                errors.Add("A question entry is null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(question.id) || string.IsNullOrWhiteSpace(question.answer) || string.IsNullOrWhiteSpace(question.question))
                errors.Add($"A question is missing id, answer, or text (id: '{question.id}').");

            if (!string.IsNullOrWhiteSpace(question.id) && !seenIds.Add(question.id))
                errors.Add($"Duplicate question id '{question.id}'.");

            if (question.startX < 0 || question.startY < 0 || question.startX >= data.gridWidth || question.startY >= data.gridHeight)
            {
                errors.Add($"Question '{question.id}' starts outside the grid at ({question.startX}, {question.startY}).");
                continue;
            }

            int x = question.startX, y = question.startY;
            foreach (char letter in question.answer.ToUpperInvariant())
            {
                if (x < 0 || y < 0 || x >= data.gridWidth || y >= data.gridHeight)
                {
                    errors.Add($"Question '{question.id}' ('{question.answer}') extends outside the grid at ({x}, {y}).");
                    break;
                }

                Vector2Int cell = new Vector2Int(x, y);
                if (grid.TryGetValue(cell, out (string Id, char Letter) existing))
                {
                    if (existing.Letter != letter)
                        errors.Add($"Crossing conflict at ({x}, {y}): '{existing.Id}' has '{existing.Letter}' but '{question.id}' has '{letter}'. The puzzle cannot be completed.");
                }
                else
                {
                    grid[cell] = (question.id ?? "?", letter);
                }

                if (question.isHorizontal) x++; else y++;
            }
        }

        return errors;
    }
}
