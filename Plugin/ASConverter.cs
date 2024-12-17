using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Shinten
{
    public static class ASConverter
    {
        struct Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;

            public override bool Equals(object obj)
            {
                if (obj is Vertex v)
                {
                    return v.position == position && v.normal == normal && v.uv == uv;
                }

                return false;
            }

            public override int GetHashCode()
            {
                int hashCode = -54916258;
                hashCode = hashCode * -1521134295 + position.GetHashCode();
                hashCode = hashCode * -1521134295 + normal.GetHashCode();
                hashCode = hashCode * -1521134295 + uv.GetHashCode();
                return hashCode;
            }
        }

        private static Vector2[] FromFloatsV2(float[] floats, int per = 2)
        {
            Vector2[] v2a = new Vector2[floats.Length / per];
            for (int i = 0; i < v2a.Length; i++)
                v2a[i] = new Vector2(floats[i * per], floats[i * per + 1]);
            return v2a;
        }

        private static Vector3[] FromFloatsV3(float[] floats, int per = 3)
        {
            Vector3[] v3a = new Vector3[floats.Length / per];
            for (int i = 0; i < v3a.Length; i++)
                v3a[i] = new Vector3(floats[i * per], floats[i * per + 1], floats[i * per + 2]);
            return v3a;
        }

        public static void ConvertMesh(AssetStudio.Mesh asMesh, Action<Mesh> callback, int subMesh = -1)
        {
            if (asMesh.m_VertexCount < 1 || asMesh.m_Vertices == null || asMesh.m_Vertices.Length != 3 * asMesh.m_VertexCount || asMesh.m_Indices == null)
                return;

            Vector3[] pos = FromFloatsV3(asMesh.m_Vertices);
            Vector3[] nrm = null;
            Vector2[] uvs = null;

            if (asMesh.m_Normals != null && asMesh.m_Normals.Length == asMesh.m_Vertices.Length)
                nrm = FromFloatsV3(asMesh.m_Normals);

            if (asMesh.m_UV0 != null && asMesh.m_UV0.Length == asMesh.m_VertexCount * 2)
                uvs = FromFloatsV2(asMesh.m_UV0);

            if (subMesh < 0)
            {
                Planety.Plugin.RunOnMainThread(() =>
                {
                    Mesh mesh = new Mesh();
                    mesh.vertices = pos;
                    if (nrm != null) mesh.normals = nrm;
                    if (uvs != null) mesh.uv = uvs;
                    mesh.triangles = asMesh.m_Indices.Select(i => (int)i).ToArray();
                    mesh.RecalculateBounds();
                    callback(mesh);
                });
            }

            int start = 0;
            for (int i = 0; i < subMesh; i++)
                start += (int)asMesh.m_SubMeshes[i].indexCount;

            AssetStudio.SubMesh asSubMesh = asMesh.m_SubMeshes[subMesh];

            List<Vertex> vertices = new List<Vertex>();
            Dictionary<Vertex, int> vertexMap = new Dictionary<Vertex, int>();
            List<int> indices = new List<int>();

            for (int i = 0; i < asSubMesh.indexCount; i++)
            {
                int ind = (int)asMesh.m_Indices[start + i];

                Vertex v = new Vertex();

                v.position = pos[ind];
                if (nrm != null) v.normal = nrm[ind];
                if (uvs != null) v.uv = uvs[ind];

                if (vertexMap.TryGetValue(v, out int exInd))
                    indices.Add(exInd);
                else
                {
                    vertexMap.Add(v, vertices.Count);
                    indices.Add(vertices.Count);
                    vertices.Add(v);
                }
            }

            Planety.Plugin.RunOnMainThread(() =>
            {
                Mesh mesh = new Mesh();
                mesh.vertices = vertices.Select(vertex => vertex.position).ToArray();
                mesh.normals = vertices.Select(vertex => vertex.normal).ToArray();
                mesh.uv = vertices.Select(vertex => vertex.uv).ToArray();
                mesh.triangles = indices.ToArray();
                mesh.RecalculateBounds();
                callback(mesh);
            });
        }
    }
}