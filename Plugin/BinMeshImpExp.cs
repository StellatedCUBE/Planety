using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Shinten
{
    static class BinMeshImpExp
    {
        public static Mesh From(BinaryReader reader, SkinnedMeshRenderer boneSource = null)
        {
            reader.ReadByte();
            byte flags = reader.ReadByte();
            bool hasNormals = (flags & 1) > 0;
            bool hasUVs = (flags & 2) > 0;
            bool hasColors = (flags & 4) > 0;
            bool hasBones = (flags & 8) > 0;
            int vertices = reader.ReadUInt16();
            int triangles3 = reader.ReadInt32();

            Vector3[] positions = new Vector3[vertices];
            Vector3[] normals = hasNormals ? new Vector3[vertices] : null;
            Vector2[] uvs = hasUVs ? new Vector2[vertices] : null;
            Color[] colors = hasColors ? new Color[vertices] : null;
            BoneWeight[] boneWeights = hasBones ? new BoneWeight[vertices] : null;
            List<Transform> bones = null;
            List<Matrix4x4> bindposes = null;
            int[] triangles = new int[triangles3];

            for (int i = 0; i < vertices; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                positions[i] = new Vector3(x, y, z);
            }

            if (hasNormals)
            {
                for (int i = 0; i < vertices; i++)
                {
                    float nx = reader.ReadSingle();
                    float ny = reader.ReadSingle();
                    float nz = reader.ReadSingle();

                    normals[i] = new Vector3(nx, ny, nz);
                }
            }

            if (hasUVs)
            {
                for (int i = 0; i < vertices; i++)
                {
                    float u = reader.ReadSingle();
                    float v = reader.ReadSingle();

                    uvs[i] = new Vector2(u, v);
                }
            }

            if (hasColors)
            {
                for (int i = 0; i < vertices; i++)
                {
                    float r = reader.ReadByte() / 255f;
                    float g = reader.ReadByte() / 255f;
                    float b = reader.ReadByte() / 255f;
                    float a = reader.ReadByte() / 255f;

                    colors[i] = new Color(r, g, b, a);
                }
            }

            if (hasBones)
            {
                byte[] mergedBoneCounts = reader.ReadBytes(((vertices - 1) >> 2) + 1);
                List<int> boneCounts = new List<int>();

                foreach (byte b in mergedBoneCounts)
                {
                    boneCounts.Add(b & 3);
                    boneCounts.Add((b >> 2) & 3);
                    boneCounts.Add((b >> 4) & 3);
                    boneCounts.Add((b >> 6) & 3);
                }

                bones = boneSource.bones.ToList();
                bindposes = boneSource.sharedMesh.bindposes.ToList();
                List<int> meshToSourceIndices = new List<int>();

                int boneCount = reader.ReadUInt16();

                for (int i = 0; i < boneCount; i++)
                {
                    string boneName = reader.ReadString();
                    int index = bones.FindIndex(bone => bone.name == boneName);
                    if (index >= 0)
                        meshToSourceIndices.Add(index);
                    else
                    {
                        Transform rootBone = bones[0];
                        while (rootBone.name != "Root")
                            rootBone = rootBone.parent;

                        Transform bone = rootBone.GetComponentsInChildren<Transform>().Where(trns => trns.name == boneName).First();

                        meshToSourceIndices.Add(bones.Count);
                        bones.Add(bone);
                        bindposes.Add(bone.worldToLocalMatrix * rootBone.parent.localToWorldMatrix);
                    }
                }

                for (int i = 0; i < vertices; i++)
                {
                    boneWeights[i].boneIndex0 = meshToSourceIndices[reader.ReadUInt16()];
                    boneWeights[i].weight0 = reader.ReadSingle();

                    if (boneCounts[i] > 0)
                    {
                        boneWeights[i].boneIndex1 = meshToSourceIndices[reader.ReadUInt16()];
                        boneWeights[i].weight1 = reader.ReadSingle();

                        if (boneCounts[i] > 1)
                        {
                            boneWeights[i].boneIndex2 = meshToSourceIndices[reader.ReadUInt16()];
                            boneWeights[i].weight2 = reader.ReadSingle();

                            if (boneCounts[i] > 2)
                            {
                                boneWeights[i].boneIndex3 = meshToSourceIndices[reader.ReadUInt16()];
                                boneWeights[i].weight3 = reader.ReadSingle();
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < triangles3; i++)
                triangles[i] = reader.ReadUInt16();

            Mesh mesh = new Mesh();

            mesh.vertices = positions;

            if (hasNormals)
                mesh.normals = normals;

            if (hasUVs)
                mesh.uv = uvs;

            if (hasColors)
                mesh.colors = colors;

            if (hasBones)
            {
                mesh.boneWeights = boneWeights;
                boneSource.sharedMesh.bindposes = mesh.bindposes = bindposes.ToArray();
                boneSource.bones = bones.ToArray();
            }

            mesh.triangles = triangles;

            mesh.RecalculateBounds();

            return mesh;
        }

        public static Mesh FromFile(string path, SkinnedMeshRenderer boneSource = null)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                return From(reader, boneSource);
            }
        }

        public static void To(Mesh mesh, BinaryWriter writer, SkinnedMeshRenderer boneSource = null)
        {
            writer.Write((byte)1);

            bool hasNormals = (from Vector3 normal in mesh.normals where normal != Vector3.zero select true).FirstOrDefault();
            bool hasUVs = (from Vector2 uv in mesh.uv where uv != Vector2.zero select true).FirstOrDefault();
            bool hasColors = (from Color color in mesh.colors where color != Color.white select true).FirstOrDefault();
            bool hasBones = boneSource != null;

            var vertexCount = mesh.vertexCount;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var colors = mesh.colors;

            //Task.Run(() =>
            //{
                writer.Write((byte)((hasNormals ? 1 : 0) + (hasUVs ? 2 : 0) + (hasColors ? 4 : 0) + (hasBones ? 8 : 0)));

                writer.Write((ushort)vertexCount);
                writer.Write(triangles.Length);

                for (int i = 0; i < vertexCount; i++)
                {
                    writer.Write(vertices[i].x);
                    writer.Write(vertices[i].y);
                    writer.Write(vertices[i].z);
                }

                if (hasNormals)
                    for (int i = 0; i < vertexCount; i++)
                    {
                        writer.Write(normals[i].x);
                        writer.Write(normals[i].y);
                        writer.Write(normals[i].z);
                    }

                if (hasUVs)
                    for (int i = 0; i < vertexCount; i++)
                    {
                        writer.Write(uvs[i].x);
                        writer.Write(uvs[i].y);
                    }

                if (hasColors)
                    for (int i = 0; i < vertexCount; i++)
                    {
                        writer.Write((byte)(colors[i].r * 255));
                        writer.Write((byte)(colors[i].g * 255));
                        writer.Write((byte)(colors[i].b * 255));
                        writer.Write((byte)(colors[i].a * 255));
                    }

                if (hasBones)
                {
                    byte[] mergedBoneCounts = new byte[((vertexCount - 1) >> 2) + 1];

                    for (int i = 0; i < vertexCount; i++)
                    {
                        BoneWeight boneWeights = mesh.boneWeights[i];
                        int boneCount = boneWeights.weight1 == 0f ? 0 : boneWeights.weight2 == 0f ? 1 : boneWeights.weight3 == 0f ? 2 : 3;
                        mergedBoneCounts[i >> 2] |= (byte)(boneCount << ((i & 3) << 1));
                    }

                    writer.Write(mergedBoneCounts);

                    writer.Write((ushort)boneSource.bones.Length);
                    foreach (Transform bone in boneSource.bones)
                        writer.Write(bone.name);

                    for (int i = 0; i < vertexCount; i++)
                    {
                        BoneWeight boneWeights = mesh.boneWeights[i];
                        int boneCount = boneWeights.weight1 == 0f ? 0 : boneWeights.weight2 == 0f ? 1 : boneWeights.weight3 == 0f ? 2 : 3;

                        writer.Write((ushort)boneWeights.boneIndex0);
                        writer.Write(boneWeights.weight0);

                        if (boneCount > 0)
                        {
                            writer.Write((ushort)boneWeights.boneIndex1);
                            writer.Write(boneWeights.weight1);

                            if (boneCount > 1)
                            {
                                writer.Write((ushort)boneWeights.boneIndex2);
                                writer.Write(boneWeights.weight3);

                                if (boneCount > 2)
                                {
                                    writer.Write((ushort)boneWeights.boneIndex3);
                                    writer.Write(boneWeights.weight3);
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < triangles.Length; i++)
                    writer.Write((ushort)triangles[i]);
            //});
        }

        public static void ToFile(Mesh mesh, string path, SkinnedMeshRenderer boneSource = null)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
            {
                To(mesh, writer, boneSource);
            }
        }
    }
}