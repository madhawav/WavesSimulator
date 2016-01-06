using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicsCore
{
    /**
    * Based on http://richardssoftware.net/ SlimDX Tutorial. Code has been modified to support SharpDX. 
    * 
    * Contains some code directly copied from the below mentioned repository belonging to author of  http://richardssoftware.net/ SlimDX Tutorial.
    * Source: https://github.com/ericrrichards/dx11/blob/master/DX11/Core/GameTimer.cs
    *
    **/
    public class GameTimer
    {
        private double _secondsPerCount;
        private double _deltaTime;
        private long _baseTime;
        private long _pausedTime;
        private long _stopTime;
        private long _prevTime;
        private long _currTime;

        private bool _stopped;

        public GameTimer()
        {
            _secondsPerCount = 0.0;
            _deltaTime = -1.0;
            _baseTime = 0;
            _pausedTime = 0;
            _prevTime = 0;
            _currTime = 0;
            _stopped = false;
            var countsPerSec = Stopwatch.Frequency;
            _secondsPerCount = 1.0 / countsPerSec;

        }

        public float TotalTime
        {
            get
            {
                if (_stopped)
                {
                    return (float)(((_stopTime - _pausedTime) - _baseTime) * _secondsPerCount);
                }
                else
                {
                    return (float)(((_currTime - _pausedTime) - _baseTime) * _secondsPerCount);
                }
            }
        }

        public float DeltaTime
        {
            get { return (float)_deltaTime; }
        }

        public void Reset()
        {
            var curTime = Stopwatch.GetTimestamp();
            _baseTime = curTime;
            _prevTime = curTime;
            _stopTime = 0;
            _stopped = false;
        }

        public void Start()
        {
            var startTime = Stopwatch.GetTimestamp();
            if (_stopped)
            {
                _pausedTime += (startTime - _stopTime);
                _prevTime = startTime;
                _stopTime = 0;
                _stopped = false;
            }
        }


        public void Stop()
        {
            if (!_stopped)
            {
                var curTime = Stopwatch.GetTimestamp();
                _stopTime = curTime;
                _stopped = true;
            }
        }



        public void Tick()
        {
            if (_stopped)
            {
                _deltaTime = 0.0;
                return;
            }
            var curTime = Stopwatch.GetTimestamp();
            _currTime = curTime;
            _deltaTime = (_currTime - _prevTime) * _secondsPerCount;
            _prevTime = _currTime;
            if (_deltaTime < 0.0)
            {
                _deltaTime = 0.0;
            }
        }
    }
}
