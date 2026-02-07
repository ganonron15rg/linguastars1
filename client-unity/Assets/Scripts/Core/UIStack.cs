using System.Collections.Generic;

namespace LinguaStars.Client.Core
{
    public class UIStack
    {
        private readonly Stack<ScreenBase> stack = new Stack<ScreenBase>();

        public int Count => stack.Count;

        public ScreenBase Peek()
        {
            return stack.Count > 0 ? stack.Peek() : null;
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void Push(ScreenBase screen)
        {
            stack.Push(screen);
        }

        public ScreenBase Pop()
        {
            return stack.Count > 0 ? stack.Pop() : null;
        }

        public IEnumerable<ScreenBase> Enumerate()
        {
            return stack;
        }
    }
}
