using Shinten;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public class SourceMeshBinFile : Source<Mesh>
    {
        readonly string path;

        public SourceMeshBinFile(string path)
        {
            this.path = path;
        }

        public Mesh Get()
        {
            if (!Plugin.IsMainThread())
            {
                var message_pass = new BlockingCollection<Mesh>(1);

                Plugin.RunOnMainThread(() => message_pass.Add(Get()));

                return message_pass.Take();
            }

            return BinMeshImpExp.FromFile(path);
        }

        public bool DisposeCreatedResource() => true;
    }
}
