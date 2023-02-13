using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace mpp_client_csharp.Channel
{
    public class Crown
    {
        public Vector2 startPos;
        public Vector2 endPos;
        public string? participantId;
        public string userId;

        public Crown(string? pid, string uid, int sx, int sy, int ex, int ey)
        {
            participantId = pid;
            userId = uid;
            startPos = new Vector2(sx, sy);
            endPos = new Vector2(ex, ey);
        }
    }
}
