using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety.UI
{
    class DelayedCall : MonoBehaviour
    {
        public Action action;

        void Update()
        {
            action();
            Destroy(this);
        }
    }
}
