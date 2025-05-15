using System;
using System.Collections.Generic;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Represents a step in the chain-of-thought reasoning process.
    /// </summary>
    public class ChainOfThoughtStep
    {
        /// <summary>
        /// The step number in the reasoning sequence.
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// The thought process for this reasoning step.
        /// </summary>
        public string Thought { get; set; } = string.Empty;

        /// <summary>
        /// The conclusion reached at this reasoning step.
        /// </summary>
        public string Conclusion { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this step has been revised during verification.
        /// </summary>
        public bool IsRevised { get; set; } = false;
    }

    /// <summary>
    /// Represents the result of chain-of-thought reasoning.
    /// </summary>
    public class ChainOfThoughtResult
    {
        /// <summary>
        /// The individual reasoning steps in the chain of thought.
        /// </summary>
        public List<ChainOfThoughtStep> ReasoningSteps { get; set; } = new List<ChainOfThoughtStep>();

        /// <summary>
        /// The final conclusion reached after the reasoning process.
        /// </summary>
        public string FinalConclusion { get; set; } = string.Empty;

        /// <summary>
        /// The confidence score for the reasoning result (0.0 to 1.0).
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Entities extracted during the reasoning process.
        /// </summary>
        public List<Entity> ExtractedEntities { get; set; } = new List<Entity>();

        /// <summary>
        /// Suggested actions based on the reasoning conclusion.
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();

        /// <summary>
        /// Indicates whether the reasoning result has been verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Indicates whether an error occurred during reasoning.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Error message if an error occurred during reasoning.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Returns the most relevant reasoning steps for a summary.
        /// </summary>
        /// <param name="maxSteps">Maximum number of steps to return.</param>
        /// <returns>A list of the most important reasoning steps.</returns>
        public List<ChainOfThoughtStep> GetKeyReasoningSteps(int maxSteps = 3)
        {
            if (ReasoningSteps.Count <= maxSteps)
            {
                return ReasoningSteps;
            }

            var result = new List<ChainOfThoughtStep>();
            
            // Always include the first step
            if (ReasoningSteps.Count > 0)
            {
                result.Add(ReasoningSteps[0]);
            }

            // Include revised steps as they're important
            foreach (var step in ReasoningSteps.FindAll(s => s.IsRevised).Take(maxSteps - 2))
            {
                if (!result.Contains(step))
                {
                    result.Add(step);
                }
            }

            // Always include the last step if we have space
            if (ReasoningSteps.Count > 1 && result.Count < maxSteps)
            {
                var lastStep = ReasoningSteps[ReasoningSteps.Count - 1];
                if (!result.Contains(lastStep))
                {
                    result.Add(lastStep);
                }
            }

            // Fill any remaining slots with evenly distributed steps
            if (result.Count < maxSteps && ReasoningSteps.Count > 2)
            {
                int remaining = maxSteps - result.Count;
                int interval = ReasoningSteps.Count / (remaining + 1);
                
                for (int i = 1; i <= remaining; i++)
                {
                    int index = i * interval;
                    if (index > 0 && index < ReasoningSteps.Count - 1)
                    {
                        var step = ReasoningSteps[index];
                        if (!result.Contains(step))
                        {
                            result.Add(step);
                        }
                    }
                }
            }

            // Sort by step number to maintain proper sequence
            result.Sort((a, b) => a.StepNumber.CompareTo(b.StepNumber));
            return result;
        }
    }
} 