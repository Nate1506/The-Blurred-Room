using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace CyberpunkSubwaySurfer
{
    public class CyberGameManager : MonoBehaviour
    {
        public static CyberGameManager Instance { get; private set; }

        // ─────────────── References ───────────────
        [Header("References")]
        public CyberPlayerController player;
        public CyberTrackGenerator   trackGenerator;
        public AudioSource           musicSource;
        public AudioSource           sfxSource;

        // ─────────────── Score & Stats ───────────────
        [Header("Score & Stats")]
        public float score = 0f;
        public int   coins = 0;
        public int   highScore = 0;
        public int   scoreMultiplier = 1;

        // ─────────────── Skill Charges ───────────────
        // Each bar goes 0..maxCharge.  Activating empties the bar.
        [Header("Skill Bars")]
        public int yellowCharge = 0;   // Adds life / immortal
        public int redCharge    = 0;   // Speed boost
        public int blueCharge   = 0;   // Slow enemies
        public int maxCharge    = 10;  // Coins needed to activate

        // ─────────────── Active Skill Flags ───────────────
        [HideInInspector] public bool isImmortal  = false;
        [HideInInspector] public bool isSpeedBoost = false;
        [HideInInspector] public bool isSlowActive = false;

        // ─────────────── Lives ───────────────
        [Header("Lives")]
        public int lives = 1;
        public int maxLives = 3;

        // ─────────────── Enemy Spawning ───────────────
        [Header("Enemy Spawning")]
        public float enemySpawnInterval = 12f;
        private float enemySpawnTimer   = 0f;

        // ─────────────── State ───────────────
        [Header("State")]
        public bool isPlaying = false;
        public bool isGameOver = false;
        public bool isPaused = false;
        public bool isMuted = false;
        private bool cameFromMainMenu = true;

        // ─────────────── UI Menus & Canvas Elements ───────────────
        [Header("UI Canvas & Panels")]
        public Canvas     hudCanvas;
        public GameObject hudContainer;
        public GameObject mainMenuPanel;
        public GameObject pauseMenuPanel;
        public GameObject howToPlayPanel;
        public GameObject gameOverPanel;

        [Header("HUD Text Elements")]
        public Text     scoreText;
        public Text     coinText;
        public Text     controlsText;
        public Text     livesText;
        public Text     skillStatusText;

        [Header("Menu Specific Text")]
        public Text     mainMenuHighScoreText;
        public Text     pauseScoreText;
        public Text     pauseCoinText;
        public Text     audioBtnMainMenuText;
        public Text     audioBtnPauseMenuText;

        // Skill bar sliders (Yellow / Red / Blue)
        public Slider   yellowBar;
        public Slider   redBar;
        public Slider   blueBar;

        // Skill key labels
        public Text     yellowLabel;
        public Text     redLabel;
        public Text     blueLabel;

        // Active-skill flash indicators
        public Image    yellowFlash;
        public Image    redFlash;
        public Image    blueFlash;

        public Text       finalScoreText;
        public Text       finalCoinText;
        public Text       highScoreText;
        public Button     restartButton;
        public Button     menuFromGameOverButton;

        // ─────────────── Internals ───────────────
        private float startZ = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            highScore = PlayerPrefs.GetInt("CyberpunkSubway_HighScore", 0);
        }

        private void Start()
        {
            if (player          == null) player          = FindFirstObjectByType<CyberPlayerController>();
            if (trackGenerator  == null) trackGenerator  = FindFirstObjectByType<CyberTrackGenerator>();

            SetupAudio();

            // Spawn procedural lightning system if not already in the scene
            if (FindFirstObjectByType<CyberLightning>() == null)
            {
                GameObject lightningGO = new GameObject("CyberLightningSystem");
                lightningGO.AddComponent<CyberLightning>();
            }

            if (hudCanvas == null) CreateCyberpunkHUD();

            ReturnToMainMenu();
        }

        // ═══════════════════════════════════════════════
        //  AUDIO SETUP
        // ═══════════════════════════════════════════════
        private void SetupAudio()
        {
            // Music AudioSource
            if (musicSource == null)
            {
                GameObject musGo = new GameObject("MusicSource");
                musGo.transform.SetParent(transform);
                musicSource = musGo.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = 0.45f;
                musicSource.spatialBlend = 0f; // 2D
                musicSource.priority = 0;
                musicSource.clip = GenerateCyberTrack();
                musicSource.Play();
            }

            // SFX AudioSource
            if (sfxSource == null)
            {
                GameObject sfxGo = new GameObject("SFXSource");
                sfxGo.transform.SetParent(transform);
                sfxSource = sfxGo.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.volume = 0.75f;
                sfxSource.spatialBlend = 0f;
            }
        }

        /// <summary>Procedurally generates a driving cyberpunk beat as an AudioClip.</summary>
        private AudioClip GenerateCyberTrack()
        {
            const int sampleRate = 44100;
            const float bpm      = 140f;
            const float beatsInLoop = 8f;
            float loopLength = (60f / bpm) * beatsInLoop; // seconds per loop
            int sampleCount  = Mathf.RoundToInt(sampleRate * loopLength);
            float[] data     = new float[sampleCount];

            float beatSec = 60f / bpm;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate; // time in seconds
                float beat = t / beatSec;         // beat number (float)
                float sample = 0f;

                // ── Kick (4-on-the-floor) ──
                // Attack at every beat
                float kickPhase = beat - Mathf.Floor(beat);
                if (kickPhase < 0.08f)
                {
                    float env = 1f - (kickPhase / 0.08f);
                    float kickFreq = Mathf.Lerp(80f, 40f, kickPhase / 0.06f);
                    sample += Mathf.Sin(kickPhase * kickFreq * 2f * Mathf.PI) * env * 0.7f;
                }

                // ── Snare / Clap (beats 2 & 4) ──
                float snarePhase = (beat - 1f) - Mathf.Floor(beat - 1f);
                int   beatIndex  = Mathf.FloorToInt(beat) % 4;
                if ((beatIndex == 1 || beatIndex == 3) && snarePhase < 0.05f)
                {
                    float env = 1f - (snarePhase / 0.05f);
                    // White-noise via hash
                    float noise = Mathf.Repeat(Mathf.Sin((float)i * 127.1f + (float)i * 311.7f) * 43758.5f, 2f) - 1f;
                    sample += noise * env * 0.4f;
                }

                // ── Hi-Hat (8th-note) ──
                float hhPhase = (beat * 2f) - Mathf.Floor(beat * 2f);
                if (hhPhase < 0.025f)
                {
                    float env = 1f - (hhPhase / 0.025f);
                    float hhNoise = Mathf.Repeat(Mathf.Sin((float)i * 337.1f) * 43758.5f, 2f) - 1f;
                    sample += hhNoise * env * 0.2f;
                }

                // ── Synth Bass Line (16 notes over 8 beats) ──
                float[] bassNotes = { 55f, 55f, 82.4f, 55f, 73.4f, 55f, 82.4f, 69.3f,
                                      55f, 55f, 82.4f, 55f, 73.4f, 61.7f, 82.4f, 55f };
                int bassStep = Mathf.FloorToInt(beat * 2f) % bassNotes.Length;
                float bassPhase = (beat * 2f) - Mathf.Floor(beat * 2f);
                if (bassPhase < 0.8f)
                {
                    float env = (bassPhase < 0.05f) ? bassPhase / 0.05f : Mathf.Clamp01(1f - (bassPhase - 0.05f) / 0.75f);
                    float freq = bassNotes[bassStep];
                    float osc  = Mathf.Sign(Mathf.Sin(t * freq * 2f * Mathf.PI)); // square wave
                    sample += osc * env * 0.25f;
                }

                // ── Arp Lead (16th notes, cyberpunk melody) ──
                float[] arpNotes = { 220f, 293.7f, 349.2f, 440f, 369.9f, 293.7f, 220f, 261.6f,
                                     220f, 293.7f, 392f,   440f, 392f,   329.6f, 261.6f, 220f };
                int arpStep  = Mathf.FloorToInt(beat * 4f) % arpNotes.Length;
                float arpPh  = (beat * 4f) - Mathf.Floor(beat * 4f);
                if (arpPh < 0.75f)
                {
                    float env  = (arpPh < 0.02f) ? arpPh / 0.02f : Mathf.Clamp01(1f - (arpPh - 0.02f) / 0.73f);
                    float freq = arpNotes[arpStep];
                    // sawtooth wave
                    float saw  = 2f * (t * freq - Mathf.Floor(t * freq + 0.5f));
                    sample += saw * env * 0.12f;
                }

                // ── Pad (sustained chord) ──
                float padEnv = 0.06f;
                sample += Mathf.Sin(t * 110f   * 2f * Mathf.PI) * padEnv;
                sample += Mathf.Sin(t * 138.6f * 2f * Mathf.PI) * padEnv;
                sample += Mathf.Sin(t * 164.8f * 2f * Mathf.PI) * padEnv;

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("CyberBeat", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Generate a short SFX clip (beep, blip, etc.).</summary>
        private AudioClip GenerateSFX(float freqHz, float durationSec, float sweep = 0f, bool noise = false)
        {
            const int sr = 44100;
            int count    = Mathf.RoundToInt(sr * durationSec);
            float[] data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t   = (float)i / sr;
                float env = Mathf.Clamp01(1f - t / durationSec);
                env = env * env; // quadratic decay
                float freq = freqHz + sweep * (t / durationSec);
                if (noise)
                {
                    float n = Mathf.Repeat(Mathf.Sin((float)i * 127.1f) * 43758.5f, 2f) - 1f;
                    data[i] = n * env;
                }
                else
                {
                    data[i] = Mathf.Sin(t * freq * 2f * Mathf.PI) * env;
                }
            }
            AudioClip clip = AudioClip.Create("SFX", count, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlaySFX(float freq, float duration, float sweep = 0f, bool noise = false)
        {
            if (sfxSource != null)
                sfxSource.PlayOneShot(GenerateSFX(freq, duration, sweep, noise));
        }

        // ═══════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════
        private void Update()
        {
            // ── Main Menu Shortcut [Space / Enter] ──
            if (!isPlaying && !isGameOver && mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                bool startPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    startPressed = Keyboard.current.spaceKey.wasPressedThisFrame ||
                                   Keyboard.current.enterKey.wasPressedThisFrame;
                }
#else
                try { startPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); }
                catch (System.Exception) {}
#endif
                if (startPressed) StartGame();
            }

            // ── In-Game Pause Toggle [ESC / P] ──
            if (isPlaying && !isGameOver)
            {
                bool pausePressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    pausePressed = Keyboard.current.escapeKey.wasPressedThisFrame ||
                                   Keyboard.current.pKey.wasPressedThisFrame;
                }
#else
                try { pausePressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P); }
                catch (System.Exception) {}
#endif
                if (pausePressed)
                {
                    if (isPaused) ResumeGame();
                    else PauseGame();
                }
            }

            // ── Active Gameplay Loop ──
            if (isPlaying && !isGameOver && !isPaused && player != null)
            {
                float distance = Mathf.Max(0f, player.transform.position.z - startZ);
                score = distance * scoreMultiplier;

                if (scoreText != null)
                    scoreText.text = $"SCORE  <color=#00f0ff>{Mathf.FloorToInt(score):D6}</color>";

                if (coinText != null)
                    coinText.text = $"CREDITS  <color=#ffd700>{coins:D3}</color>";

                if (livesText != null)
                    livesText.text = $"LIVES  <color=#ff3399>{lives}</color>";

                // Skill bars
                if (yellowBar != null) yellowBar.value = (float)yellowCharge / maxCharge;
                if (redBar    != null) redBar.value    = (float)redCharge    / maxCharge;
                if (blueBar   != null) blueBar.value   = (float)blueCharge   / maxCharge;

                // Enemy spawning
                enemySpawnTimer += Time.deltaTime;
                if (enemySpawnTimer >= enemySpawnInterval)
                {
                    enemySpawnTimer = 0f;
                    SpawnEnemy();
                }

                // Skill key inputs
                HandleSkillInput();
            }

            // ── Game Over Shortcuts [Space / R to restart, ESC / M for main menu] ──
            if (isGameOver)
            {
                bool restartPressed = false;
                bool menuPressed    = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    restartPressed = Keyboard.current.spaceKey.wasPressedThisFrame ||
                                     Keyboard.current.rKey.wasPressedThisFrame    ||
                                     Keyboard.current.enterKey.wasPressedThisFrame;
                    menuPressed    = Keyboard.current.escapeKey.wasPressedThisFrame ||
                                     Keyboard.current.mKey.wasPressedThisFrame;
                }
#else
                try
                {
                    restartPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return);
                    menuPressed    = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M);
                }
                catch (System.Exception) {}
#endif
                if (restartPressed) RestartGame();
                else if (menuPressed) ReturnToMainMenu();
            }
        }

        private void HandleSkillInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) ActivateYellowSkill();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) ActivateRedSkill();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) ActivateBlueSkill();
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ActivateYellowSkill();
                if (Input.GetKeyDown(KeyCode.Alpha2)) ActivateRedSkill();
                if (Input.GetKeyDown(KeyCode.Alpha3)) ActivateBlueSkill();
            }
            catch (System.Exception) {}
#endif
        }

        // ═══════════════════════════════════════════════
        //  COIN / SKILL CHARGE
        // ═══════════════════════════════════════════════
        public void AddCoinByType(CoinType type, int amount)
        {
            coins += amount;
            PlaySFX(880f, 0.08f, 440f);

            switch (type)
            {
                case CoinType.Yellow: yellowCharge = Mathf.Min(yellowCharge + amount, maxCharge); break;
                case CoinType.Red:    redCharge    = Mathf.Min(redCharge    + amount, maxCharge); break;
                case CoinType.Blue:   blueCharge   = Mathf.Min(blueCharge   + amount, maxCharge); break;
            }

            FlashBarIndicator(type);
            UpdateSkillLabels();
        }

        // Legacy support
        public void AddCoin(int amount) => AddCoinByType(CoinType.Yellow, amount);

        // ═══════════════════════════════════════════════
        //  SKILL ACTIVATION
        // ═══════════════════════════════════════════════

        // ── Yellow [1]: +1 Life → at 10 charges becomes Immortal (5s) ──
        private void ActivateYellowSkill()
        {
            if (!isPlaying || isGameOver || yellowCharge < maxCharge) return;
            yellowCharge = 0;

            if (lives < maxLives)
            {
                lives++;
                ShowSkillStatus("EXTRA LIFE GAINED!", new Color(1f, 0.9f, 0.1f));
                PlaySFX(660f, 0.3f, 220f);
            }
            else
            {
                // Max lives → grant Immortality
                StartCoroutine(ImmortalRoutine(5f));
                ShowSkillStatus("IMMORTAL  (5s)", new Color(1f, 0.9f, 0.1f));
                PlaySFX(440f, 0.5f, 440f);
            }
        }

        // ── Red [2]: Speed Boost (6s) ──
        private void ActivateRedSkill()
        {
            if (!isPlaying || isGameOver || redCharge < maxCharge) return;
            redCharge = 0;
            StartCoroutine(SpeedBoostRoutine(6f));
            ShowSkillStatus("SPEED BOOST  (6s)", new Color(1f, 0.2f, 0.3f));
            PlaySFX(330f, 0.25f, 660f);
        }

        // ── Blue [3]: Slow Enemies (8s) ──
        private void ActivateBlueSkill()
        {
            if (!isPlaying || isGameOver || blueCharge < maxCharge) return;
            blueCharge = 0;
            StartCoroutine(SlowEnemyRoutine(8f));
            ShowSkillStatus("ENEMIES SLOWED  (8s)", new Color(0.1f, 0.7f, 1f));
            PlaySFX(220f, 0.4f, -100f);
        }

        // ── Immortal Coroutine ──
        private IEnumerator ImmortalRoutine(float duration)
        {
            isImmortal = true;
            if (yellowFlash != null)
            {
                yellowFlash.gameObject.SetActive(true);
                StartCoroutine(FlashImage(yellowFlash, duration));
            }
            yield return new WaitForSeconds(duration);
            isImmortal = false;
            if (yellowFlash != null) yellowFlash.gameObject.SetActive(false);
        }

        // ── Speed Boost Coroutine ──
        private IEnumerator SpeedBoostRoutine(float duration)
        {
            isSpeedBoost = true;
            if (player != null)
            {
                player.maxForwardSpeed += 14f;
                player.currentForwardSpeed = Mathf.Min(player.currentForwardSpeed + 10f, player.maxForwardSpeed);
            }
            if (redFlash != null)
            {
                redFlash.gameObject.SetActive(true);
                StartCoroutine(FlashImage(redFlash, duration));
            }
            yield return new WaitForSeconds(duration);
            isSpeedBoost = false;
            if (player != null) player.maxForwardSpeed -= 14f;
            if (redFlash != null) redFlash.gameObject.SetActive(false);
        }

        // ── Slow Enemy Coroutine ──
        private IEnumerator SlowEnemyRoutine(float duration)
        {
            isSlowActive = true;
            if (blueFlash != null)
            {
                blueFlash.gameObject.SetActive(true);
                StartCoroutine(FlashImage(blueFlash, duration));
            }
            // Apply slow to every active enemy
            foreach (CyberEnemy enemy in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
            {
                enemy.SlowDown(duration);
            }
            yield return new WaitForSeconds(duration);
            isSlowActive = false;
            if (blueFlash != null) blueFlash.gameObject.SetActive(false);
        }

        private IEnumerator FlashImage(Image img, float totalTime)
        {
            float t = 0f;
            while (t < totalTime)
            {
                float alpha = 0.4f + 0.4f * Mathf.Sin(t * 8f);
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
                t += Time.deltaTime;
                yield return null;
            }
        }

        private void ShowSkillStatus(string msg, Color c)
        {
            if (skillStatusText == null) return;
            skillStatusText.text  = msg;
            skillStatusText.color = c;
            StopCoroutine("HideStatusRoutine");
            StartCoroutine(HideStatusRoutine(2.5f));
        }

        private IEnumerator HideStatusRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (skillStatusText != null) skillStatusText.text = "";
        }

        private void UpdateSkillLabels()
        {
            if (yellowLabel != null)
                yellowLabel.text = $"[1] LIFE / IMMORTAL\n{yellowCharge}/{maxCharge}";
            if (redLabel != null)
                redLabel.text = $"[2] SPEED BOOST\n{redCharge}/{maxCharge}";
            if (blueLabel != null)
                blueLabel.text = $"[3] SLOW ENEMIES\n{blueCharge}/{maxCharge}";
        }

        private void FlashBarIndicator(CoinType type)
        {
            // Brief colour pulse on the slider handle — simple alpha trick
            Image target = null;
            switch (type)
            {
                case CoinType.Yellow: target = yellowFlash; break;
                case CoinType.Red:    target = redFlash;    break;
                case CoinType.Blue:   target = blueFlash;   break;
            }
            if (target != null)
            {
                target.gameObject.SetActive(true);
                StartCoroutine(QuickFlash(target));
            }
        }

        private IEnumerator QuickFlash(Image img)
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0.85f);
            yield return new WaitForSeconds(0.15f);
            img.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════
        //  ENEMY SPAWNING
        // ═══════════════════════════════════════════════
        private void SpawnEnemy()
        {
            if (player == null) return;
            int spawnLane = Random.Range(0, 3);
            float spawnZ  = player.transform.position.z + 60f;
            float spawnX  = (spawnLane - 1) * 2.8f;
            float spawnY  = 1.0f;
            CyberEnemy.SpawnAt(new Vector3(spawnX, spawnY, spawnZ), spawnLane);
        }

        // ═══════════════════════════════════════════════
        //  GAME FLOW & MENUS
        // ═══════════════════════════════════════════════
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            isPaused   = false;
            isPlaying  = false;
            isGameOver = false;

            // Destroy all active enemies
            foreach (CyberEnemy e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                Destroy(e.gameObject);

            if (trackGenerator != null) trackGenerator.ResetGenerator();
            if (player != null) { player.ResetPlayer(); startZ = player.transform.position.z; }

            // Hide in-game HUD and overlays
            if (hudContainer   != null) hudContainer.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (gameOverPanel  != null) gameOverPanel.SetActive(false);

            // Show Main Menu
            if (mainMenuPanel  != null) mainMenuPanel.SetActive(true);
            UpdateMainMenuHighScore();
            UpdateAudioButtonLabels();

            if (musicSource != null && !musicSource.isPlaying) musicSource.Play();
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            isPlaying   = true;
            isGameOver  = false;
            isPaused    = false;
            score       = 0f;
            coins       = 0;
            lives       = 1;
            yellowCharge = 0;
            redCharge    = 0;
            blueCharge   = 0;
            enemySpawnTimer = 0f;

            if (player != null)
            {
                player.ResetPlayer();
                startZ = player.transform.position.z;
            }

            // Destroy all active enemies
            foreach (CyberEnemy e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                Destroy(e.gameObject);

            if (trackGenerator != null) trackGenerator.ResetGenerator();

            // Hide menus, show HUD
            if (mainMenuPanel  != null) mainMenuPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (gameOverPanel  != null) gameOverPanel.SetActive(false);
            if (hudContainer   != null) hudContainer.SetActive(true);

            if (skillStatusText != null) skillStatusText.text = "";
            UpdateSkillLabels();

            if (musicSource != null && !musicSource.isPlaying) musicSource.Play();
        }

        public void PauseGame()
        {
            if (!isPlaying || isGameOver || isPaused) return;
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
                if (pauseScoreText != null)
                    pauseScoreText.text = $"SCORE: <color=#00f0ff>{Mathf.FloorToInt(score):D6}</color>";
                if (pauseCoinText != null)
                    pauseCoinText.text = $"CREDITS: <color=#ffd700>{coins:D3}</color>";
            }

            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            UpdateAudioButtonLabels();
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        }

        public void OpenHowToPlay(bool fromMainMenu)
        {
            cameFromMainMenu = fromMainMenu;
            if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
            if (fromMainMenu && mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (!fromMainMenu && pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        public void CloseHowToPlay()
        {
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (cameFromMainMenu)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            }
            else
            {
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            }
        }

        public void ToggleAudio()
        {
            isMuted = !isMuted;
            if (musicSource != null) musicSource.mute = isMuted;
            if (sfxSource   != null) sfxSource.mute   = isMuted;
            UpdateAudioButtonLabels();
        }

        private void UpdateAudioButtonLabels()
        {
            string label = isMuted ? "♫  AUDIO: MUTED" : "♫  AUDIO: ON";
            Color col = isMuted ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.2f, 0.95f, 0.4f);
            if (audioBtnMainMenuText != null)
            {
                audioBtnMainMenuText.text = label;
                audioBtnMainMenuText.color = col;
            }
            if (audioBtnPauseMenuText != null)
            {
                audioBtnPauseMenuText.text = label;
                audioBtnPauseMenuText.color = col;
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void GameOver()
        {
            if (isGameOver) return;

            // Lives system — if we have extra lives, use one instead of dying
            if (lives > 1)
            {
                lives--;
                ShowSkillStatus($"LIFE LOST — {lives} REMAINING", new Color(1f, 0.3f, 0.8f));
                PlaySFX(110f, 0.4f, -50f, true);

                // Revive player
                if (player != null)
                {
                    player.isDead = false;
                    if (player.visualRoot != null)
                    {
                        player.visualRoot.localPosition = Vector3.zero;
                        player.visualRoot.localRotation = Quaternion.identity;
                        player.visualRoot.localScale    = Vector3.one;
                    }
                    // Give brief immortality after revive
                    StartCoroutine(ImmortalRoutine(2f));
                }
                return;
            }

            isGameOver = true;
            isPlaying  = false;
            isPaused   = false;
            Time.timeScale = 1f;

            // Stop music, play crash sfx
            if (musicSource != null) musicSource.Stop();
            PlaySFX(150f, 0.6f, -100f, true);

            int intScore = Mathf.FloorToInt(score);
            if (intScore > highScore)
            {
                highScore = intScore;
                PlayerPrefs.SetInt("CyberpunkSubway_HighScore", highScore);
                PlayerPrefs.Save();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {intScore:D6}";
                if (finalCoinText  != null) finalCoinText.text  = $"CREDITS EXTRACTED: {coins}";
                if (highScoreText  != null) highScoreText.text  = $"HIGH SCORE: {highScore:D6}";
            }
        }

        public void RestartGame()
        {
            // Destroy all enemies
            foreach (CyberEnemy e in FindObjectsByType<CyberEnemy>(FindObjectsSortMode.None))
                Destroy(e.gameObject);

            if (trackGenerator != null) trackGenerator.ResetGenerator();
            if (player         != null) { player.ResetPlayer(); startZ = player.transform.position.z; }

            if (musicSource != null && !musicSource.isPlaying) musicSource.Play();
            StartGame();
        }

        private void UpdateMainMenuHighScore()
        {
            if (mainMenuHighScoreText != null)
            {
                mainMenuHighScoreText.text = $"BEST RECORD  <color=#ffd700>{highScore:D6}</color>";
            }
        }

        // ═══════════════════════════════════════════════
        //  HUD & MENU CREATION
        // ═══════════════════════════════════════════════
        private void CreateCyberpunkHUD()
        {
            // ── Canvas ──
            GameObject canvasGo = new GameObject("CyberpunkHUD_Canvas");
            canvasGo.transform.SetParent(transform);
            hudCanvas = canvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }

            Font fnt = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── In-Game HUD Container ──
            hudContainer = new GameObject("InGameHUD");
            hudContainer.transform.SetParent(canvasGo.transform, false);
            RectTransform hudRt = hudContainer.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero;
            hudRt.anchorMax = Vector2.one;
            hudRt.sizeDelta = Vector2.zero;

            // ── Top Bar ──
            RectTransform topRt = MakeRect(hudContainer.transform, "TopBar",
                new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0.5f,1f),
                new Vector2(0f,-20f), new Vector2(-60f, 60f));

            // Score (top-left)
            scoreText = MakeText(topRt.gameObject, "ScoreText", fnt, 28, FontStyle.Bold,
                new Vector2(0f,0.5f), new Vector2(0f,0.5f), new Vector2(0f,0.5f),
                new Vector2(20f,0f), new Vector2(320f,50f),
                Color.white, TextAnchor.MiddleLeft, true);
            scoreText.text = "SCORE  <color=#00f0ff>000000</color>";

            // Lives (top-center)
            livesText = MakeText(topRt.gameObject, "LivesText", fnt, 26, FontStyle.Bold,
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0f,0f), new Vector2(200f,50f),
                new Color(1f,0.3f,0.6f), TextAnchor.MiddleCenter, true);
            livesText.text = "LIVES  <color=#ff3399>1</color>";

            // Coins (top-right, shifted left to leave room for Pause button)
            coinText = MakeText(topRt.gameObject, "CoinText", fnt, 28, FontStyle.Bold,
                new Vector2(1f,0.5f), new Vector2(1f,0.5f), new Vector2(1f,0.5f),
                new Vector2(-150f,0f), new Vector2(240f,50f),
                Color.white, TextAnchor.MiddleRight, true);
            coinText.text = "CREDITS  <color=#ffd700>000</color>";

            // HUD Pause Button (top-right)
            CreateCyberButton(topRt, "PauseButton", "|| PAUSE", fnt,
                new Vector2(-50f, 0f), new Vector2(110f, 42f),
                new Color(0f, 0.9f, 1f), Color.white,
                () => PauseGame());

            // ── Skill Bars (bottom-left panel) ──
            RectTransform skillPanel = MakeRect(hudContainer.transform, "SkillPanel",
                new Vector2(0f,0f), new Vector2(0f,0f), new Vector2(0f,0f),
                new Vector2(20f, 160f), new Vector2(260f, 150f));
            Image panelBg = skillPanel.gameObject.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0.05f, 0.65f);

            BuildSkillBar(skillPanel, fnt, 0, "LIFE / IMMO [1]", new Color(1f, 0.9f, 0.1f), out yellowBar, out yellowLabel, out yellowFlash);
            BuildSkillBar(skillPanel, fnt, 1, "SPEED BOOST [2]",  new Color(1f, 0.2f, 0.3f), out redBar,    out redLabel,    out redFlash);
            BuildSkillBar(skillPanel, fnt, 2, "SLOW ENEMY  [3]",  new Color(0.1f, 0.7f, 1f), out blueBar,   out blueLabel,   out blueFlash);

            // ── Skill Status (center screen) ──
            skillStatusText = MakeText(hudContainer, "SkillStatus", fnt, 28, FontStyle.Bold,
                new Vector2(0.5f,0.6f), new Vector2(0.5f,0.6f), new Vector2(0.5f,0.5f),
                Vector2.zero, new Vector2(700f,50f),
                new Color(1f,0.9f,0f), TextAnchor.MiddleCenter, true);
            skillStatusText.text = "";

            // ── Controls (bottom center) ──
            controlsText = MakeText(hudContainer, "ControlsText", fnt, 18, FontStyle.Normal,
                new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(0.5f,0f),
                new Vector2(0f,25f), new Vector2(700f,40f),
                new Color(0.7f,0.9f,1f,0.85f), TextAnchor.MiddleCenter, true);
            controlsText.text = "<b>[A/D]</b> Lane  |  <b>[Space/W]</b> Jump  |  <b>[S]</b> Slide  |  <b>[1][2][3]</b> Skills  |  <b>[ESC]</b> Pause";

            // ── Menus ──
            BuildMainMenu(canvasGo.transform, fnt);
            BuildPauseMenu(canvasGo.transform, fnt);
            BuildHowToPlayModal(canvasGo.transform, fnt);
            BuildGameOverPanel(canvasGo.transform, fnt);

            hudContainer.SetActive(false);
            pauseMenuPanel.SetActive(false);
            howToPlayPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            mainMenuPanel.SetActive(true);

            UpdateSkillLabels();
        }

        private void BuildMainMenu(Transform parent, Font fnt)
        {
            mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(parent, false);
            RectTransform mmRt = mainMenuPanel.AddComponent<RectTransform>();
            mmRt.anchorMin = new Vector2(0.5f, 0.5f);
            mmRt.anchorMax = new Vector2(0.5f, 0.5f);
            mmRt.pivot     = new Vector2(0.5f, 0.5f);
            mmRt.sizeDelta = new Vector2(540f, 560f);

            // Background card
            Image bg = mainMenuPanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.09f, 0.95f);

            // Glowing top cyan line
            GameObject topBar = new GameObject("TopGlow");
            topBar.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform tbRt = topBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 4f);
            topBar.AddComponent<Image>().color = new Color(0f, 0.95f, 1f);

            // Glowing bottom magenta line
            GameObject btmBar = new GameObject("BtmGlow");
            btmBar.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform bbRt = btmBar.AddComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot     = new Vector2(0.5f, 0f);
            bbRt.sizeDelta = new Vector2(0f, 3f);
            btmBar.AddComponent<Image>().color = new Color(1f, 0.05f, 0.55f);

            // Title
            Text titleTxt = MakeText(mainMenuPanel, "Title", fnt, 40, FontStyle.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -45f), new Vector2(520f, 50f),
                new Color(0f, 0.95f, 1f), TextAnchor.MiddleCenter, false);
            titleTxt.text = "CYBERPUNK RUNNER";

            // Subtitle
            Text subTxt = MakeText(mainMenuPanel, "Subtitle", fnt, 15, FontStyle.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -85f), new Vector2(520f, 30f),
                new Color(1f, 0.15f, 0.6f), TextAnchor.MiddleCenter, false);
            subTxt.text = "// HIGHWAY PROTOCOL v2.0 //";

            // High Score Banner
            mainMenuHighScoreText = MakeText(mainMenuPanel, "HighScoreText", fnt, 18, FontStyle.Normal,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -125f), new Vector2(400f, 35f),
                Color.white, TextAnchor.MiddleCenter, true);
            UpdateMainMenuHighScore();

            // Buttons
            CreateCyberButton(mainMenuPanel.transform, "StartButton", "▶  START RUN  [SPACE]", fnt,
                new Vector2(0f, 40f), new Vector2(340f, 52f),
                new Color(0f, 0.95f, 1f), new Color(0.95f, 0.95f, 1f),
                StartGame);

            CreateCyberButton(mainMenuPanel.transform, "HowToPlayButton", "?  HOW TO PLAY", fnt,
                new Vector2(0f, -25f), new Vector2(340f, 48f),
                new Color(1f, 0.15f, 0.65f), Color.white,
                () => OpenHowToPlay(true));

            Button audioBtn = CreateCyberButton(mainMenuPanel.transform, "AudioButton", "♫  AUDIO: ON", fnt,
                new Vector2(0f, -85f), new Vector2(340f, 48f),
                new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.95f, 0.4f),
                ToggleAudio);
            audioBtnMainMenuText = audioBtn.GetComponentInChildren<Text>();

            CreateCyberButton(mainMenuPanel.transform, "QuitButton", "✕  QUIT GAME", fnt,
                new Vector2(0f, -145f), new Vector2(340f, 48f),
                new Color(1f, 0.25f, 0.25f), new Color(1f, 0.6f, 0.6f),
                QuitGame);

            // Footer hint
            Text footerTxt = MakeText(mainMenuPanel, "Footer", fnt, 13, FontStyle.Normal,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 25f), new Vector2(500f, 25f),
                new Color(0.6f, 0.85f, 1f, 0.75f), TextAnchor.MiddleCenter, false);
            footerTxt.text = "PRESS [SPACE] OR CLICK TO JACK IN";
        }

        private void BuildPauseMenu(Transform parent, Font fnt)
        {
            pauseMenuPanel = new GameObject("PauseMenuPanel");
            pauseMenuPanel.transform.SetParent(parent, false);
            RectTransform pmRt = pauseMenuPanel.AddComponent<RectTransform>();
            pmRt.anchorMin = new Vector2(0.5f, 0.5f);
            pmRt.anchorMax = new Vector2(0.5f, 0.5f);
            pmRt.pivot     = new Vector2(0.5f, 0.5f);
            pmRt.sizeDelta = new Vector2(480f, 520f);

            // Background card
            Image bg = pauseMenuPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.96f);

            // Glowing top amber line
            GameObject topBar = new GameObject("TopGlow");
            topBar.transform.SetParent(pauseMenuPanel.transform, false);
            RectTransform tbRt = topBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 4f);
            topBar.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f);

            // Title
            Text titleTxt = MakeText(pauseMenuPanel, "Title", fnt, 32, FontStyle.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -42f), new Vector2(450f, 45f),
                new Color(1f, 0.85f, 0.1f), TextAnchor.MiddleCenter, false);
            titleTxt.text = "// SYSTEM PAUSED //";

            // Current Stats
            pauseScoreText = MakeText(pauseMenuPanel, "PauseScore", fnt, 19, FontStyle.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -85f), new Vector2(420f, 30f),
                Color.white, TextAnchor.MiddleCenter, true);

            pauseCoinText = MakeText(pauseMenuPanel, "PauseCoins", fnt, 17, FontStyle.Normal,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -115f), new Vector2(420f, 30f),
                new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, true);

            // Buttons
            CreateCyberButton(pauseMenuPanel.transform, "ResumeBtn", "▶  RESUME RUN  [ESC]", fnt,
                new Vector2(0f, 45f), new Vector2(320f, 50f),
                new Color(0f, 0.95f, 1f), Color.white,
                ResumeGame);

            CreateCyberButton(pauseMenuPanel.transform, "RestartBtn", "↺  REBOOT RUN  [R]", fnt,
                new Vector2(0f, -15f), new Vector2(320f, 46f),
                new Color(1f, 0.15f, 0.6f), Color.white,
                RestartGame);

            CreateCyberButton(pauseMenuPanel.transform, "HowToBtn", "?  HOW TO PLAY", fnt,
                new Vector2(0f, -70f), new Vector2(320f, 46f),
                new Color(1f, 0.85f, 0.1f), Color.white,
                () => OpenHowToPlay(false));

            Button audioBtn = CreateCyberButton(pauseMenuPanel.transform, "AudioBtn", "♫  AUDIO: ON", fnt,
                new Vector2(0f, -125f), new Vector2(320f, 46f),
                new Color(0.2f, 0.95f, 0.4f), Color.white,
                ToggleAudio);
            audioBtnPauseMenuText = audioBtn.GetComponentInChildren<Text>();

            CreateCyberButton(pauseMenuPanel.transform, "MenuBtn", "⌂  MAIN MENU", fnt,
                new Vector2(0f, -180f), new Vector2(320f, 46f),
                new Color(0.6f, 0.7f, 0.85f), Color.white,
                ReturnToMainMenu);
        }

        private void BuildHowToPlayModal(Transform parent, Font fnt)
        {
            howToPlayPanel = new GameObject("HowToPlayPanel");
            howToPlayPanel.transform.SetParent(parent, false);
            RectTransform htpRt = howToPlayPanel.AddComponent<RectTransform>();
            htpRt.anchorMin = new Vector2(0.5f, 0.5f);
            htpRt.anchorMax = new Vector2(0.5f, 0.5f);
            htpRt.pivot     = new Vector2(0.5f, 0.5f);
            htpRt.sizeDelta = new Vector2(740f, 580f);

            // Background card
            Image bg = howToPlayPanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);

            // Glowing top cyan line
            GameObject topBar = new GameObject("TopGlow");
            topBar.transform.SetParent(howToPlayPanel.transform, false);
            RectTransform tbRt = topBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 4f);
            topBar.AddComponent<Image>().color = new Color(0f, 0.95f, 1f);

            // Title
            Text titleTxt = MakeText(howToPlayPanel, "Title", fnt, 28, FontStyle.Bold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -38f), new Vector2(700f, 40f),
                new Color(0f, 0.95f, 1f), TextAnchor.MiddleCenter, false);
            titleTxt.text = "// PROTOCOL MANUAL //";

            // Body content
            Text bodyTxt = MakeText(howToPlayPanel, "Body", fnt, 15, FontStyle.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(680f, 380f),
                Color.white, TextAnchor.UpperLeft, true);
            bodyTxt.text =
                "<b><color=#00f0ff>[ 1. RUNNER LOCOMOTION ]</color></b>\n" +
                "  • <b>[A] / [D]</b> or <b>[Left] / [Right]</b> : Switch track lanes\n" +
                "  • <b>[W] / [Space]</b> or <b>[Up]</b> : Leap obstacles & vault train ramps\n" +
                "  • <b>[S]</b> or <b>[Down]</b> : Cyber Slide under laser gates\n\n" +
                "<b><color=#ffd700>[ 2. CYBER COINS & SKILL CORES ]</color></b>\n" +
                "  • <color=#ffd700>YELLOW COIN [1]</color> : Adds +1 Extra Life! At max lives, triggers <b>5s IMMORTAL SHIELD</b>.\n" +
                "  • <color=#ff3366>RED COIN [2]</color> : <b>NEON OVERDRIVE</b> — Super speed boost & score multiplier for 6s.\n" +
                "  • <color=#00d0ff>BLUE COIN [3]</color> : <b>EMP SHOCKWAVE</b> — Slows incoming enemy hunter drones to 30% speed for 8s.\n\n" +
                "<b><color=#ff0055>[ 3. THREAT ADVISORY ]</color></b>\n" +
                "  • Head-on obstacle hits cause instant reboot (unless Immortal shield is active).\n" +
                "  • Watch out for oncoming Cyber Drones hunting in lanes!";

            // Close button
            CreateCyberButton(howToPlayPanel.transform, "CloseBtn", "✕  RETURN", fnt,
                new Vector2(0f, -235f), new Vector2(240f, 46f),
                new Color(0f, 0.95f, 1f), Color.white,
                CloseHowToPlay);
        }

        private void BuildSkillBar(RectTransform parent, Font fnt, int index,
                                    string labelStr, Color barColor,
                                    out Slider slider, out Text label, out Image flash)
        {
            float rowH   = 44f;
            float yOff   = 120f - index * rowH;
            float padX   = 10f;
            float barW   = 240f;

            // Label
            label = MakeText(parent.gameObject, $"SkillLabel{index}", fnt, 13, FontStyle.Bold,
                new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(0f,1f),
                new Vector2(padX, -yOff + 2f), new Vector2(barW, 16f),
                barColor, TextAnchor.MiddleLeft, false);
            label.text = labelStr;

            // Slider background
            GameObject sliderGo = new GameObject($"SkillSlider{index}");
            sliderGo.transform.SetParent(parent, false);
            RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f,1f);
            sliderRt.anchorMax = new Vector2(0f,1f);
            sliderRt.pivot     = new Vector2(0f,1f);
            sliderRt.anchoredPosition = new Vector2(padX, -yOff - 16f);
            sliderRt.sizeDelta = new Vector2(barW, 18f);

            // BG
            GameObject bgGo = new GameObject("BG");
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f,0.1f,0.15f,0.9f);

            // Fill Area
            GameObject fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform faRt = fillAreaGo.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(2f, 2f);
            faRt.offsetMax = new Vector2(-2f, -2f);

            // Fill
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f,0f);
            fillRt.anchorMax = new Vector2(1f,1f);
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = barColor;

            slider = sliderGo.AddComponent<Slider>();
            slider.fillRect  = fillRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue  = 0f;
            slider.maxValue  = 1f;
            slider.value     = 0f;
            slider.interactable = false; // display only

            // Flash overlay (hidden by default)
            GameObject flashGo = new GameObject($"Flash{index}");
            flashGo.transform.SetParent(sliderGo.transform, false);
            RectTransform flashRt = flashGo.AddComponent<RectTransform>();
            flashRt.anchorMin = Vector2.zero; flashRt.anchorMax = Vector2.one;
            flashRt.sizeDelta = Vector2.zero;
            flash = flashGo.AddComponent<Image>();
            flash.color = new Color(barColor.r, barColor.g, barColor.b, 0f);
            flashGo.SetActive(false);
        }

        private void BuildGameOverPanel(Transform parent, Font fnt)
        {
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(parent, false);
            RectTransform goRt = gameOverPanel.AddComponent<RectTransform>();
            goRt.anchorMin = new Vector2(0.5f,0.5f);
            goRt.anchorMax = new Vector2(0.5f,0.5f);
            goRt.pivot     = new Vector2(0.5f,0.5f);
            goRt.sizeDelta = new Vector2(500f,420f);
            gameOverPanel.AddComponent<Image>().color = new Color(0.05f,0.07f,0.12f,0.96f);

            // Top red glow line
            GameObject topBar = new GameObject("TopGlow");
            topBar.transform.SetParent(gameOverPanel.transform, false);
            RectTransform tbRt = topBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 4f);
            topBar.AddComponent<Image>().color = new Color(1f, 0.05f, 0.45f);

            // Title
            Text titleTxt = MakeText(gameOverPanel, "Title", fnt, 32, FontStyle.Bold,
                new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0.5f,1f),
                new Vector2(0f,-40f), new Vector2(450f,50f),
                new Color(1f,0.05f,0.45f), TextAnchor.MiddleCenter, false);
            titleTxt.text = "SYSTEM FAILURE";

            finalScoreText = MakeText(gameOverPanel, "FinalScore", fnt, 22, FontStyle.Bold,
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0f, 65f), new Vector2(420f,35f), Color.white, TextAnchor.MiddleCenter, false);

            finalCoinText = MakeText(gameOverPanel, "FinalCoins", fnt, 18, FontStyle.Normal,
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0f, 25f), new Vector2(420f,30f),
                new Color(1f,0.85f,0.2f), TextAnchor.MiddleCenter, false);

            highScoreText = MakeText(gameOverPanel, "HighScore", fnt, 18, FontStyle.Normal,
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0f,-15f), new Vector2(420f,30f),
                new Color(0f,0.95f,1f), TextAnchor.MiddleCenter, false);

            // Reboot button
            restartButton = CreateCyberButton(gameOverPanel.transform, "RestartButton", "↺  REBOOT RUN  [SPACE]", fnt,
                new Vector2(0f, -80f), new Vector2(300f, 48f),
                new Color(0f, 0.95f, 1f), Color.white,
                RestartGame);

            // Main Menu button
            menuFromGameOverButton = CreateCyberButton(gameOverPanel.transform, "MenuButton", "⌂  MAIN MENU  [ESC]", fnt,
                new Vector2(0f, -140f), new Vector2(300f, 46f),
                new Color(1f, 0.15f, 0.55f), Color.white,
                ReturnToMainMenu);
        }

        // ═══════════════════════════════════════════════
        //  UI HELPERS
        // ═══════════════════════════════════════════════
        private Button CreateCyberButton(Transform parent, string name, string text, Font fnt,
            Vector2 pos, Vector2 size, Color accentColor, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            RectTransform btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot     = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = size;

            // Background
            Image bgImg = btnGo.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.13f, 0.95f);

            // Left accent bar
            GameObject barGo = new GameObject("AccentBar");
            barGo.transform.SetParent(btnGo.transform, false);
            RectTransform barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot     = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(6f, 0f);
            Image barImg = barGo.AddComponent<Image>();
            barImg.color = accentColor;

            // Top outline line
            GameObject topBorder = new GameObject("Border");
            topBorder.transform.SetParent(btnGo.transform, false);
            RectTransform tbRt = topBorder.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 1f);
            tbRt.sizeDelta = new Vector2(0f, 1.5f);
            Image tbImg = topBorder.AddComponent<Image>();
            tbImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(0.25f, 0.4f, 0.6f, 1f);
            cb.pressedColor     = accentColor;
            cb.selectedColor    = cb.highlightedColor;
            btn.colors = cb;

            Text btnTxt = MakeText(btnGo, "BtnText", fnt, 17, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(6f, 0f), Vector2.zero,
                textColor, TextAnchor.MiddleCenter, false);
            btnTxt.text = text;

            btn.onClick.AddListener(() =>
            {
                PlaySFX(900f, 0.06f, 300f);
                onClick?.Invoke();
            });

            return btn;
        }

        private RectTransform MakeRect(Transform parent, string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 piv,
            Vector2 aPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = piv;
            rt.anchoredPosition = aPos; rt.sizeDelta = size;
            return rt;
        }

        private Text MakeText(GameObject parent, string name, Font fnt, int size, FontStyle style,
            Vector2 ancMin, Vector2 ancMax, Vector2 piv, Vector2 aPos, Vector2 sz,
            Color color, TextAnchor align, bool richText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = piv;
            rt.anchoredPosition = aPos; rt.sizeDelta = sz;
            Text t = go.AddComponent<Text>();
            t.font = fnt; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = align; t.supportRichText = richText;
            return t;
        }
    }
}
