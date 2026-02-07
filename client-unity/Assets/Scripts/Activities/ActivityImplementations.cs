using System;
using System.Collections.Generic;

namespace LinguaStars.Client.Activities
{
    public class HearTapActivity : ActivityBase
    {
        public HearTapActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return string.Equals(submission.Response, Definition.correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class MatchPairsActivity : ActivityBase
    {
        public MatchPairsActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            if (submission.PairMatches == null || Definition.pairLeft == null || Definition.pairRight == null)
            {
                return false;
            }

            if (Definition.pairLeft.Length != Definition.pairRight.Length)
            {
                return false;
            }

            for (int i = 0; i < Definition.pairLeft.Length; i++)
            {
                string left = Definition.pairLeft[i];
                string right = Definition.pairRight[i];
                if (!submission.PairMatches.TryGetValue(left, out string matched) || matched != right)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class BuildWordActivity : ActivityBase
    {
        public BuildWordActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return string.Equals(submission.Response, Definition.correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ChooseCorrectActivity : ActivityBase
    {
        public ChooseCorrectActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return string.Equals(submission.Response, Definition.correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class BuildSentenceActivity : ActivityBase
    {
        public BuildSentenceActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            if (submission.Responses == null || submission.Responses.Count == 0)
            {
                return false;
            }

            string sentence = string.Join(" ", submission.Responses).Trim();
            return string.Equals(sentence, Definition.targetSentence, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ListenChooseActivity : ActivityBase
    {
        public ListenChooseActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return string.Equals(submission.Response, Definition.correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ListenTypeActivity : ActivityBase
    {
        public ListenTypeActivity(ActivityDefinition definition) : base(definition) { }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return string.Equals(submission.Response, Definition.correctAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class SpeakActivity : ActivityBase
    {
        public SpeakActivity(ActivityDefinition definition) : base(definition)
        {
            IsPlayable = false;
        }

        protected override bool EvaluateSubmission(ActivitySubmission submission)
        {
            return false;
        }
    }

    public static class ActivityFactory
    {
        public static ActivityBase Create(ActivityDefinition definition)
        {
            switch (definition.type)
            {
                case ActivityType.HearTap:
                    return new HearTapActivity(definition);
                case ActivityType.MatchPairs:
                    return new MatchPairsActivity(definition);
                case ActivityType.BuildWord:
                    return new BuildWordActivity(definition);
                case ActivityType.ChooseCorrect:
                    return new ChooseCorrectActivity(definition);
                case ActivityType.BuildSentence:
                    return new BuildSentenceActivity(definition);
                case ActivityType.ListenChoose:
                    return new ListenChooseActivity(definition);
                case ActivityType.ListenType:
                    return new ListenTypeActivity(definition);
                case ActivityType.Speak:
                    return new SpeakActivity(definition);
                default:
                    throw new ArgumentOutOfRangeException(nameof(definition.type), definition.type, "Unsupported activity type.");
            }
        }
    }
}
