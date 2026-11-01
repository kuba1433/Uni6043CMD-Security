using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using System.ComponentModel;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Xml;
using System.Security.Cryptography;


/*
 * NOTE: there is a problem where Microsoft.Data.Sqlite keeps being innaccesible causing compile errors. To fix this uninstall and reinstall Microsoft.Data.Sqlite in version 9.0.4 in NuGet Packages
 */
namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server.Startup();
            Server.Loop();
            Server.Shutdown();
        }
    }

    /*
     * The code below is an unused iteration of retrieving prefab information.
     * The idea was that the client would send all of its networked gameObjects prefabs including meaningful attributes, like collider sizes, however ended up not being necessary.
     * This would be a good system for a step before full authoratitative server, because then players would send across their prefabs to be used from other clients, meaning clients didn't have to understand exactly what the other client had to even connect and represent each other
     * Theoretically a player could have a cosmetic DLC and another wouldn't need even need it inside of the project to be seen from their perspective
     * 
     * This was scrapped due to benig more effort than its worth
     */

    ///// <summary>
    ///// CLIENT SENDS: HEADER COMPONENT1 (attempts id ('int'), however 'string' if unknown COMPONENT2 ...
    ///// </summary>
    //class GetPrefabIdPacket : Packet
    //{
    //    Prefab[] aprefAll;
    //    public GetPrefabIdPacket()
    //    {
    //        sHeader = "GetPrefabID";
    //    }

    //    protected override void NewMessage(string sMessage)
    //    {
    //        aprefAll = Server.GetAllPrefabs();
    //    }

    //    //protected override string PacketResponse(string sMessage)
    //    //{
    //    //    string[] asParams = DeterminePacketParameters(sMessage);
    //    //    int[] aiThisCompList = new int[asParams.Length];
    //    //    for(int i = 0; i < aiThisCompList.Length; i++)
    //    //    {
    //    //        aiThisCompList[i] = Prefab.GetComponentIdCanBeInt(asParams[i]);
    //    //    }
    //    //    for(int i = 0; i < aprefAll.Length; i++)
    //    //    {
    //    //        aprefAll[i].CompareComponents(aiThisCompList);
    //    //    }
    //    //}
    //}

    ///// <summary>
    ///// CLIENT SENDS : HEADER NAME ENUM1 ENUM2 ...
    ///// </summary>
    //class PushEnumPacket : Packet
    //{
    //    public PushEnumPacket()
    //    {
    //        sHeader = "PushEnum";
    //    }

    //    protected override void NewMessage(string sMessage)
    //    {
    //        string[] asParams = DeterminePacketParameters(sMessage);
    //        string[] asEnumParts = new string[asParams.Length - 1];
    //        for(int i = 0; i < asEnumParts.Length; i++)
    //        {
    //            asEnumParts[i] = asParams[i + 1];
    //        }
    //        new ComponentEnum(asParams[0], asEnumParts);
    //    }

    //    protected override bool PacketSendCheck(string sMessage, Client cliReceiver)
    //    {
    //        return false;
    //    }
    //}


    //class Prefab
    //{
    //    int iId;
    //    int[] aiComponents = new int[0];
    //    static List<Component> lcompAll = new List<Component>();
    //    static int iPointer = 0;
    //    public Prefab(int iThisId, int[] aiComps)
    //    {
    //        iId = GetNewId();
    //        aiComponents = aiComps;
    //    }

    //    public Prefab(int iThisId, string[] asComps)
    //    {
    //        iId = GetNewId();
    //        aiComponents = new int[asComps.Length];
    //        for(int i = 0; i < asComps.Length; i++)
    //        {
    //            aiComponents[i] = GetComponentId(asComps[i]);
    //        }
    //    }

    //    int GetNewId()
    //    {
    //        int iOut = iPointer;
    //        iPointer++;
    //        return iOut;
    //    }

    //    public static int GetComponentId(string sFull)
    //    {
    //        string sNameToSearch = sFull.Split(new string[1] { Server.sSeperator }, StringSplitOptions.RemoveEmptyEntries)[0];
    //        bool bExists = false;
    //        for(int i = 0; i < lcompAll.Count; i++)
    //        {
    //            if ((string)lcompAll[i] == )
    //        }
    //    }

    //    public static int GetComponentIdCanBeInt(string sThisComp)
    //    {
    //        int iResult;
    //        return int.TryParse(sThisComp, out iResult) ? iResult : GetComponentId(sThisComp);
    //    }

    //    public bool CompareComponents(int[] aiCompIds)
    //    {
    //        if(aiCompIds.Length != aiComponents.Length)
    //        {
    //            return false;
    //        }
    //        for(int i = 0; i < aiCompIds.Length; i++)
    //        {
    //            bool bFound = false;
    //            for(int j = 0; j < aiComponents.Length; j++)
    //            {
    //                if (aiComponents[j] == aiCompIds[i])
    //                {
    //                    bFound = true;
    //                    break;
    //                }
    //            }
    //            if(!bFound)
    //            {
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    public static explicit operator int[](Prefab prefA) => prefA.aiComponents;
    //}

    ///// <summary>
    ///// NAME PARAMCOUNT PARAM1TYPE PARAM1 PARAM2TYPE PARAM2
    ///// </summary>
    //public class Component
    //{
    //    string sName;
    //    ComponentParameter[] comppImportant = new ComponentParameter[0];
    //    public Component(string sN)
    //    {
    //        sName = sN;
    //    }

    //    public static explicit operator string(Component compA) => compA.sName;
    //}

    //public abstract class ComponentParameter
    //{
    //}

    //public class ComponentEnumParameter : ComponentParameter
    //{
    //    ComponentEnum compEUsed;
    //}

    //public class ComponentEnum
    //{
    //    string sName;
    //    string[] asEnums;
    //    static List<ComponentEnum> lcompEAll = new List<ComponentEnum>();

    //    public ComponentEnum(string sN, string[] asAll)
    //    {
    //        sName = sN;
    //        asEnums = asAll;
    //        lcompEAll.Add(this);
    //    }

    //    public int GetValue(string sFind)
    //    {
    //        for(int i = 0; i < asEnums.Length; i++)
    //        {
    //            if (asEnums[i] == sFind)
    //            {
    //                return i;
    //            }
    //        }
    //        return 0;
    //    }

    //    public static KeyValuePair<bool, int> Find(string sNameToSearch)
    //    {
    //        for(int i = 0; i < lcompEAll.Count; i++)
    //        {
    //            if ((string)lcompEAll[i] == sNameToSearch)
    //            {
    //                return new KeyValuePair<bool, int>(true, i);
    //            }
    //        }
    //        return new KeyValuePair<bool, int>(false, 0);
    //    }

    //    public static explicit operator string(ComponentEnum compEA) => compEA.sName;
    //}
}