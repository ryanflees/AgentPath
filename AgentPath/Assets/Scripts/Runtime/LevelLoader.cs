using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CR
{
    public class LevelLoader : MonoBehaviourSingleton<LevelLoader>
    {
        [SerializeField] private CanvasGroup m_FadeGroup;
        [SerializeField] private float m_FadeDuration = 0.5f;

        private bool m_IsLoading = false;

        protected override void OnAwake()
        {
            base.OnAwake();
            DontDestroyOnLoad(gameObject);
            if (m_FadeGroup != null) m_FadeGroup.alpha = 0f;
        }

        public void LoadLevel(int chapter, int level, bool checkUnlock = true)
        {
            if (m_IsLoading) return;
            if (checkUnlock && !UserDataManager.Instance.IsUnlocked(chapter, level)) return;

            StartCoroutine(LoadSequence(chapter, level));
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void LoadNextLevel()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            string[] parts = sceneName.Split('_');
            if (parts.Length < 3) return;

            if (int.TryParse(parts[1], out int chapter) && int.TryParse(parts[2], out int level))
            {
                var config = MetaGame.GetConfig<LevelDataConfig>();
                int nextLevel = level + 1;
                
                // 获取当前章节的关卡总数
                int currentChapterMax = (chapter == 1) ? config.m_Chapter1.Count : config.m_Chapter2.Count;

                if (nextLevel <= currentChapterMax)
                {
                    LoadLevel(chapter, nextLevel);
                }
                else
                {
                    // 章节完成，尝试进入下一章或返回主菜单
                    if (chapter == 1)
                    {
                        LoadLevel(2, 1);
                    }
                    else
                    {
                        LoadMainMenu();
                    }
                }
            }
        }

        public void ReloadCurrent()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator LoadSequence(int chapter, int level)
        {
            m_IsLoading = true;
            yield return StartCoroutine(Fade(1.0f));

            string nextScene = $"level_{chapter:D2}_{level:D4}";
            AsyncOperation op = SceneManager.LoadSceneAsync(nextScene);
            while (!op.isDone) yield return null;

            yield return StartCoroutine(Fade(0.0f));
            m_IsLoading = false;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            if (m_FadeGroup == null) yield break;
            float startAlpha = m_FadeGroup.alpha;
            float time = 0;
            while (time < m_FadeDuration)
            {
                time += Time.deltaTime;
                m_FadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / m_FadeDuration);
                yield return null;
            }
            m_FadeGroup.alpha = targetAlpha;
        }
    }
}