using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.GameScene
{
    enum NoteType
    {
        Tap = 0,
        Hold = 1,
        Slide = 2,
        Flick = 3
    }

    public class Note
    {
        // miliseconds
        private readonly float time;
        private readonly int type;
        private readonly int lane;

    }
}
