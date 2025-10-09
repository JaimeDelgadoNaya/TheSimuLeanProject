using System;
using System.Collections.Generic;
using System.Linq;

namespace ChapasGA.GA
{
    /// <summary>
    /// A utility class for sequence optimization tasks, including transforming
    /// sequences based on priorities and adding labels to data dictionaries.
    /// </summary>
    public static class SeqOptTools
    {
        /// <summary>
        /// Reorders the values within each key of a dictionary based on a list of priorities.
        /// The priorities represent the new position for each element (1-based indexing).
        /// </summary>
        /// <param name="dataDict">The dictionary to reorder (column name -> list of values)</param>
        /// <param name="priorities">The list of priorities indicating new positions (1-based)</param>
        /// <returns>A new dictionary with reordered values</returns>
        /// <exception cref="ArgumentException">If priorities don't match data length or contain invalid values</exception>
        public static Dictionary<string, List<string>> TransformSequence(
            Dictionary<string, List<string>> dataDict, 
            IList<int> priorities)
        {
            if (dataDict == null || dataDict.Count == 0)
                throw new ArgumentException("Data dictionary cannot be null or empty");

            if (priorities == null || priorities.Count == 0)
                throw new ArgumentException("Priorities list cannot be null or empty");

            // Validate that all values in the dictionary match the length of priorities
            int expectedLength = priorities.Count;
            foreach (var kvp in dataDict)
            {
                if (kvp.Value.Count != expectedLength)
                {
                    throw new ArgumentException(
                        $"Length mismatch for key '{kvp.Key}': values have length {kvp.Value.Count}, " +
                        $"priorities have length {expectedLength}");
                }
            }

            // Validate priorities are in valid range (1 to N)
            var minPriority = priorities.Min();
            var maxPriority = priorities.Max();
            if (minPriority < 1 || maxPriority > expectedLength)
            {
                throw new ArgumentException(
                    $"Priorities must be in range [1, {expectedLength}], " +
                    $"but found range [{minPriority}, {maxPriority}]");
            }

            // Create the reordered dictionary
            var reorderedDict = new Dictionary<string, List<string>>();

            foreach (var kvp in dataDict)
            {
                string key = kvp.Key;
                List<string> values = kvp.Value;
                List<string> reorderedValues = new List<string>(new string[expectedLength]);

                for (int currentIndex = 0; currentIndex < priorities.Count; currentIndex++)
                {
                    int priorityPosition = priorities[currentIndex];
                    
                    // Validate priority position
                    if (priorityPosition < 1 || priorityPosition > expectedLength)
                    {
                        throw new IndexOutOfRangeException(
                            $"Priority position {priorityPosition} is out of range for priorities list");
                    }

                    // Place the current value into the new position (adjust for 0-based indexing)
                    reorderedValues[priorityPosition - 1] = values[currentIndex];
                }

                reorderedDict[key] = reorderedValues;
            }

            return reorderedDict;
        }

        /// <summary>
        /// Adds a new label and its corresponding values to the dictionary.
        /// If the label already exists, it overwrites its values.
        /// </summary>
        /// <param name="dataDict">The data dictionary to update</param>
        /// <param name="newLabelName">The new label name to add</param>
        /// <param name="newLabelValues">The list of values for the new label</param>
        /// <returns>The updated dictionary (same reference as input)</returns>
        /// <exception cref="ArgumentException">If the length doesn't match existing entries</exception>
        public static Dictionary<string, List<string>> AddLabelsToDict(
            Dictionary<string, List<string>> dataDict,
            string newLabelName,
            IList<int> newLabelValues)
        {
            if (dataDict == null)
                throw new ArgumentNullException(nameof(dataDict));

            if (string.IsNullOrEmpty(newLabelName))
                throw new ArgumentException("Label name cannot be null or empty");

            if (newLabelValues == null)
                throw new ArgumentNullException(nameof(newLabelValues));

            // Check if the length matches existing entries
            if (dataDict.Count > 0)
            {
                var firstKey = dataDict.Keys.First();
                int expectedLength = dataDict[firstKey].Count;
                
                if (newLabelValues.Count != expectedLength)
                {
                    throw new ArgumentException(
                        $"The length of newLabelValues ({newLabelValues.Count}) does not match " +
                        $"the length of other entries ({expectedLength}).");
                }
            }

            // Convert int list to string list and add/overwrite
            dataDict[newLabelName] = newLabelValues.Select(v => v.ToString()).ToList();

            return dataDict;
        }

        /// <summary>
        /// Converts a Dictionary&lt;string, List&lt;string&gt;&gt; (column-oriented)
        /// to a List&lt;Dictionary&lt;string, string&gt;&gt; (row-oriented).
        /// This is useful for creating items from the reordered data.
        /// </summary>
        /// <param name="dataDict">Column-oriented dictionary</param>
        /// <returns>Row-oriented list of dictionaries</returns>
        public static List<Dictionary<string, string>> ConvertToRowOriented(
            Dictionary<string, List<string>> dataDict)
        {
            if (dataDict == null || dataDict.Count == 0)
                return new List<Dictionary<string, string>>();

            int rowCount = dataDict.Values.First().Count;
            var result = new List<Dictionary<string, string>>(rowCount);

            for (int i = 0; i < rowCount; i++)
            {
                var row = new Dictionary<string, string>();
                foreach (var kvp in dataDict)
                {
                    row[kvp.Key] = kvp.Value[i];
                }
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Creates a deep copy of a dictionary of string lists.
        /// </summary>
        public static Dictionary<string, List<string>> DeepCopy(
            Dictionary<string, List<string>> original)
        {
            if (original == null)
                return null;

            var copy = new Dictionary<string, List<string>>();
            foreach (var kvp in original)
            {
                copy[kvp.Key] = new List<string>(kvp.Value);
            }
            return copy;
        }

        /// <summary>
        /// Validates that a dictionary has consistent column lengths.
        /// </summary>
        public static bool ValidateConsistentLength(Dictionary<string, List<string>> dataDict)
        {
            if (dataDict == null || dataDict.Count == 0)
                return true;

            int? expectedLength = null;
            foreach (var kvp in dataDict)
            {
                if (expectedLength == null)
                {
                    expectedLength = kvp.Value.Count;
                }
                else if (kvp.Value.Count != expectedLength)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
