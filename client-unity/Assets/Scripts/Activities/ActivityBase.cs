using System;
using UnityEngine;

namespace LinguaStars.Client.Activities
{
    public abstract class ActivityBase
    {
        protected ActivityBase(ActivityDefinition definition)
        {
            Definition = definition;
        }

        public ActivityDefinition Definition { get; }
        public bool IsPlayable { get; protected set; } = true;

        public event Action<ActivityBase> Started;
        public event Action<ActivityResult> Completed;
        public event Action<ActivityResult> Failed;
        public event Action<ActivityBase> HintRequested;

        private float startTime;
        private int attempts;
        private bool finished;

        public void StartActivity()
        {
            if (finished)
            {
                return;
            }

            startTime = Time.realtimeSinceStartup;
            Started?.Invoke(this);
        }

        public ActivityResult SubmitAnswer(ActivitySubmission submission)
        {
            if (finished || submission == null)
            {
                return null;
            }

            attempts += 1;
            bool correct = EvaluateSubmission(submission);
            if (correct)
            {
                return Complete(true);
            }

            if (attempts == 1)
            {
                HintRequested?.Invoke(this);
                return null;
            }

            return Complete(false);
        }

        protected abstract bool EvaluateSubmission(ActivitySubmission submission);

        private ActivityResult Complete(bool success)
        {
            finished = true;
            float duration = Mathf.Max(1f, Time.realtimeSinceStartup - startTime);
            int wrongCount = success ? attempts - 1 : attempts;
            int correctCount = success ? 1 : 0;
            float accuracy = attempts > 0 ? (float)correctCount / attempts : 0f;

            ActivityResult result = new ActivityResult
            {
                ActivityId = Definition.activityId,
                Type = Definition.type,
                ItemId = Definition.itemId,
                Success = success,
                UsedRetry = success && attempts > 1,
                Attempts = attempts,
                CorrectCount = correctCount,
                WrongCount = wrongCount,
                Accuracy = accuracy,
                DurationSeconds = duration
            };

            if (success)
            {
                Completed?.Invoke(result);
            }
            else
            {
                Failed?.Invoke(result);
            }

            return result;
        }
    }
}
