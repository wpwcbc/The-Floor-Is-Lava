using UnityEngine;

public sealed class FollowOriginLogic : IPatternUpdateLogic
{
    private readonly PatternInstance _leader;
    private readonly bool _syncFrame;

    private bool _loggedMissingLeader;

    public FollowOriginLogic(PatternInstance leader, bool syncFrame)
    {
        _leader = leader;
        _syncFrame = syncFrame;
        _loggedMissingLeader = false;
    }

    public void Tick(PatternInstance instance, float deltaTime)
    {
        if (instance == null)
        {
            return;
        }

        if (_leader == null)
        {
            if (!_loggedMissingLeader)
            {
                Debug.LogError("[FollowOriginLogic] Leader is null.");
                _loggedMissingLeader = true;
            }
            return;
        }

        instance.SetOrigin(_leader.Origin);

        if (_syncFrame)
        {
            instance.SetFrame(_leader.CurrentFrameIndex);
        }
    }
}
