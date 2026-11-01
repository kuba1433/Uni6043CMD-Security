using ConsoleApp1.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Stores information about objects in the scene to organise them across clients
    /// </summary>
    class GameObject
    {
        int iId;
        int iLocalId;  // the localID that 'cliOwner' specified for this object
        Vector3 v3Pos = Vector3.Zero;
        Vector4 v4Rot = Vector4.Zero;
        Client cliOwner;
        int iPrefabId;

        public GameObject(int x, int y, int z)
        {
            iId = x;
            iPrefabId = y;
            iLocalId = z;
        }

        /// <summary>
        /// Sets the transform for this game object and the client if this object is the player object
        /// </summary>
        /// <param name="v3NewPos"></param>
        /// <param name="v4NewRot"></param>
        /// <returns>true if the input position and rotation are identitical to what it was previously</returns>
        public bool NewTransform(Vector3 v3NewPos, Vector4 v4NewRot)
        {
            if(GetPrefabID() == 0)
            {
                cliOwner.UpdateLastPosition(v3NewPos);
            }
            if (v3NewPos == v3Pos && v4NewRot == v4Rot)
            {
                return true;
            }
            v3Pos = v3NewPos;
            v4Rot = v4NewRot;
            return false;
        }

        public Client AttachClient(Client cliNew)
        {
            Client cliTemp = cliOwner;
            cliOwner = cliNew;
            return cliTemp;
        }

        public int GetID() => iId;
        public int GetLocalID() => iLocalId;
        public int GetPrefabID() => iPrefabId;

        public static explicit operator Vector3(GameObject goA) => goA.v3Pos;
        public static explicit operator Vector4(GameObject goA) => goA.v4Rot;
        public static explicit operator Client(GameObject goA) => goA.cliOwner;
    }
}
