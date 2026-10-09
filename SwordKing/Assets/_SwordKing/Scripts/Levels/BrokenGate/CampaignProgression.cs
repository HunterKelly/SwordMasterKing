using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwordKing
{
    public partial class BrokenGateLevel
    {
        // A scene handoff, not an additional persistent save. Starting a new
        // chapter resets its encounters while retaining the current sword build.
        static SaveData chapterTransfer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetChapterTransfer() { chapterTransfer=null; }

        void BeginNextChapter()
        {
            if(screen==ScreenState.Transition) return;
            if(!Application.CanStreamedLevelBeLoaded("IceWorld"))
            {
                saveWarning="Add IceWorld to your active Build Profile scene list to continue.";
                SetScreen(ScreenState.Victory); return;
            }
            Save();
            chapterTransfer=new SaveData { power=Player.power, recovery=Player.recovery, speed=Player.speed, shards=save.shards };
            SetScreen(ScreenState.Transition);
            StartCoroutine(EnterNextChapter());
        }
        IEnumerator EnterNextChapter()
        {
            // Let the completion message register; enemies and gameplay input
            // remain blocked during the scene change.
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale=1;
            var loading=SceneManager.LoadSceneAsync("IceWorld",LoadSceneMode.Single);
            if(loading!=null) yield return loading;
        }
    }
}
