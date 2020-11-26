using Commons.Utils;

namespace Commons.Game
{
    public class Shoot
    {
        private int _shooter;
        private int _hitted;
        private float _timeToBeDiscarded;

        public Shoot(int shooter, int hitted)
        {
            _shooter = shooter;
            _hitted = hitted;
            _timeToBeDiscarded = 2f;
        }

        public bool MustBeDiscardedByTimeout(float newDeltaTime)
        {
            _timeToBeDiscarded -= newDeltaTime;
            return _timeToBeDiscarded < 0;
        }

        public int Shooter => _shooter;

        public int Hitted => _hitted;

        public float TimeToBeDiscarded => _timeToBeDiscarded;
    }
}