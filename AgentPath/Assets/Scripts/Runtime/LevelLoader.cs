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
        private int m_PendingChapter;
        private int m_PendingLevel;

        protected override void OnAwake()
        {
            base.OnAwake();
            DontDestroyOnLoad(gameObject);
            if (m_FadeGroup != null) m_FadeGroup.alpha = 0f;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void LoadLevel(int chapter, int level, bool checkUnlock = true)
        {
            if (m_IsLoading) return;
            if (checkUnlock && !UserDataManager.Instance.IsUnlocked(chapter, level)) return;

            m_PendingChapter = chapter;
            m_PendingLevel = level;
            
            StartCoroutine(LoadSequence());
        }

        private IEnumerator LoadSequence()
        {
            m_IsLoading = true;
            yield return StartCoroutine(Fade(1.0f));

            LevelDataConfig config = MetaGame.GetConfig<LevelDataConfig>();
            if (config)
            {
                Resources.UnloadUnusedAssets();
                GameObject prefab = GetLevelPrefab(config, m_PendingChapter, m_PendingLevel);
                if (prefab)
                {
                    Resources.LoadAsync<GameObject>($"Levels/{prefab.name}");
                }
            }
            AsyncOperation op = SceneManager.LoadSceneAsync("Gameplay");
            while (!op.isDone) yield return null;
            
            //yield return StartCoroutine(Fade(0.0f));
            m_IsLoading = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Gameplay") return;

            LevelDataConfig config = MetaGame.GetConfig<LevelDataConfig>();
            if (config == null) return;

            GameObject prefab = GetLevelPrefab(config, m_PendingChapter, m_PendingLevel);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = $"Level_{m_PendingChapter:D2}_{m_PendingLevel:D4}";
            }
            StartCoroutine(FadeDelay(0.3f));
        }

        public void LoadNextLevel()
        {
            LevelDataConfig config = MetaGame.GetConfig<LevelDataConfig>();
            if (config == null) return;

            int nextLevel = m_PendingLevel + 1;
            int nextChapter = m_PendingChapter;

            bool hasNextInChapter = IsLevelExists(config, nextChapter, nextLevel);

            if (hasNextInChapter)
            {
                LoadLevel(nextChapter, nextLevel);
            }
            else
            {
                nextChapter++;
                nextLevel = 1;

                if (IsLevelExists(config, nextChapter, nextLevel))
                {
                    LoadLevel(nextChapter, nextLevel);
                }
                else
                {
                    LoadMainMenu();
                }
            }
        }

        private string GetLevelName(int chapter, int level)
        {
            string targetName = $"level_{chapter:D2}_{level:D4}";
            return targetName;
        }

        private bool IsLevelExists(LevelDataConfig config, int chapter, int level)
        {
            List<LevelDataConfig.LevelData> list = (chapter == 1) ? config.m_Chapter1 : config.m_Chapter2;
            if (list == null) return false;

            string targetName = GetLevelName(chapter, level);
            return list.Exists(x => x.m_LevelSceneName == targetName);
        }

        private GameObject GetLevelPrefab(LevelDataConfig config, int chapter, int level)
        {
            List<LevelDataConfig.LevelData> list = (chapter == 1) ? config.m_Chapter1 : config.m_Chapter2;
            string targetName = $"level_{chapter:D2}_{level:D4}";
            
            var data = list.Find(x => x.m_LevelSceneName == targetName);
            return data?.m_Prefab;
        }
        
        private IEnumerator FadeDelay(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            yield return StartCoroutine(Fade(0.0f));
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

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}