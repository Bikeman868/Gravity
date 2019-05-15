using System;
using System.Collections.Generic;

namespace Gravity.Server.Ui.Shapes
{
    public class ConnectionPoint
    {
        private readonly List<Action<float, float>> _subscribers;
        private readonly Func<Tuple<float, float>> _getAbsolutePoistion;

        public ConnectionPoint(Func<Tuple<float, float>> getAbsolutePoistion)
        {
            _getAbsolutePoistion = getAbsolutePoistion;
            _subscribers = new List<Action<float, float>>();
        }

        public void Subscribe(Action<float, float> subscriber)
        {
            _subscribers.Add(subscriber);
        }

        public void Moved()
        {
            var absolutePosition = _getAbsolutePoistion();

            foreach (var subscriber in _subscribers)
                subscriber(absolutePosition.Item1, absolutePosition.Item2);
        }
    }
}