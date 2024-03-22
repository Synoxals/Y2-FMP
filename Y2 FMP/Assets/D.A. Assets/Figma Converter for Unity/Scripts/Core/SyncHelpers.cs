using DA_Assets.FCU.Model;
using DA_Assets.Shared;
using DA_Assets.Shared.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SyncHelpers : MonoBehaviourBinder<FigmaConverterUnity>
    {
        public IEnumerator DestroySyncHelpers()
        {
            SyncHelper[] syncHelpers = GetSyncHelpers();

            for (int i = 0; i < syncHelpers.Length; i++)
            {
                syncHelpers[i].Destroy();
                yield return null;
            }

            DALogger.Log(FcuLocKey.log_current_canvas_metas_destroy.Localize(
                monoBeh.GetInstanceID(),
                syncHelpers.Length,
                nameof(SyncHelper)));
        }

        public SyncHelper[] GetAllSyncHelpers()
        {
            try
            {
                SyncHelper[] onSceneSyncHelpers = null;
#if UNITY_2020_1_OR_NEWER
                onSceneSyncHelpers = MonoBehaviour.FindObjectsOfType<SyncHelper>(true);
#else
                onSceneSyncHelpers = Resources.FindObjectsOfTypeAll<SyncHelper>();
#endif
                SyncHelper[] currentInstanceHelpers = onSceneSyncHelpers
                    .Where(x => x.Data.FigmaConverterUnity.GetInstanceID() == monoBeh.GetInstanceID())
                    .ToArray();

                return currentInstanceHelpers;
            }
            catch
            {
                return new List<SyncHelper>().ToArray();
            }
        }

        public bool IsExistsOnCurrentCanvas(FObject fobject, out SyncHelper syncObject)
        {
            SyncHelper[] syncHelpers = GetSyncHelpers();

            foreach (SyncHelper sh in syncHelpers)
            {
                if (sh.Data.Id == fobject.Id)
                {
                    syncObject = sh;
                    return true;
                }
            }

            syncObject = null;
            return false;
        }


        public SyncHelper[] GetSyncHelpers()
        {
            List<SyncHelper> syncHelpers = monoBeh.gameObject.GetComponentsInChilds<SyncHelper>();

            if (syncHelpers.IsEmpty())
            {
                return new List<SyncHelper>().ToArray();
            }
            else
            {
                return syncHelpers.ToArray();
            }
        }

        public IEnumerator SetFcuToAllSyncHelpers()
        {
            int counter = 0;
            SetFcuToAllChilds(monoBeh.gameObject, ref counter);

            yield return WaitFor.Delay01();

            DALogger.Log(FcuLocKey.log_fcu_assigned.Localize(
                counter,
                nameof(FigmaConverterUnity),
                monoBeh.GetInstanceID()));
        }

        public void RestoreRootFrames(SyncHelper[] syncHelpers)
        {
            foreach (SyncHelper syncHelper in syncHelpers)
            {
                SyncData myRootFrame = GetRootFrame(syncHelper.Data);
                syncHelper.Data.RootFrame = myRootFrame;
            }

            SyncData GetRootFrame(SyncData syncData)
            {
                GameObject currentGameObject = syncData.GameObject;

                while (currentGameObject != null)
                {
                    SyncHelper syncHelper = currentGameObject.GetComponent<SyncHelper>();
                    if (syncHelper != null && syncHelper.Data.Tags.Contains(FcuTag.Frame))
                    {
                        return syncHelper.Data;
                    }

                    currentGameObject = currentGameObject.transform.parent?.gameObject;
                }

                return null;
            }
        }

        public void SetFcuToAllChilds(GameObject @object, ref int counter)
        {
            if (@object == null)
                return;

            foreach (Transform child in @object.transform)
            {
                if (child == null)
                    continue;

                if (child.TryGetComponent(out SyncHelper syncObject))
                {
                    counter++;
                    syncObject.Data.FigmaConverterUnity = monoBeh;
                }

                SetFcuToAllChilds(child.gameObject, ref counter);
            }
        }
    }
}