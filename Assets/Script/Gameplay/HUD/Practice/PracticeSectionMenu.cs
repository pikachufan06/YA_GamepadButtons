﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YARG.Core.Chart;
using YARG.Core.Input;
using YARG.Menu.Navigation;

namespace YARG.Gameplay.HUD
{
    public class PracticeSectionMenu : GameplayBehaviour
    {
        private const int SECTION_VIEW_EXTRA = 10;
        private const float SCROLL_TIME = 1f / 60f;

        private PauseMenuManager _pauseMenuManager;

        private bool _navigationPushed = false;

        private List<Section> _sections;
        public IReadOnlyList<Section> Sections => _sections;

        [SerializeField]
        private Transform _sectionContainer;
        [SerializeField]
        private Scrollbar _scrollbar;

        [Space]
        [SerializeField]
        private GameObject _sectionViewPrefab;

        private readonly List<PracticeSectionView> _sectionViews = new();

        private int _hoveredIndex;
        public int HoveredIndex
        {
            get => _hoveredIndex;
            private set
            {
                // Properly wrap the value
                if (value < 0)
                {
                    _hoveredIndex = _sections.Count - 1;
                }
                else if (value >= _sections.Count)
                {
                    _hoveredIndex = 0;
                }
                else
                {
                    _hoveredIndex = value;
                }

                UpdateSectionViews();
            }
        }


        public int? FirstSelectedIndex { get; private set; }
        public int? LastSelectedIndex  { get; private set; }

        private float _scrollTimer;

        private uint _finalTick;
        private double _finalChartTime;

        protected override void GameplayAwake()
        {
            _pauseMenuManager = FindObjectOfType<PauseMenuManager>();

            if (!GameManager.IsPractice)
            {
                Destroy(gameObject);
                return;
            }

            GameManager.ChartLoaded += OnChartLoaded;

            // Create all of the section views
            for (int i = 0; i < SECTION_VIEW_EXTRA * 2 + 1; i++)
            {
                int relativeIndex = i - SECTION_VIEW_EXTRA;
                var gameObject = Instantiate(_sectionViewPrefab, _sectionContainer);

                var sectionView = gameObject.GetComponent<PracticeSectionView>();
                sectionView.Init(relativeIndex, this);
                _sectionViews.Add(sectionView);
            }
        }

        private void OnEnable()
        {
            // Wait until the chart has been loaded
            if (_sections is null) return;

            Initialize();
        }

        private void OnDisable()
        {
            if (_navigationPushed)
            {
                Navigator.Instance.PopScheme();
                _navigationPushed = false;
            }
        }

        protected override void OnChartLoaded(SongChart chart)
        {
            _sections = chart.Sections;
            _finalTick = chart.GetLastTick();
            _finalChartTime = chart.SyncTrack.TickToTime(_finalTick);

            //_pauseMenuManager.PushMenu(PauseMenuManager.Menu.SelectSections);
        }

        private void Initialize()
        {
            FirstSelectedIndex = null;
            LastSelectedIndex = null;
            RegisterNavigationScheme();
            UpdateSectionViews();
        }

        private void UpdateSectionViews()
        {
            foreach (var sectionView in _sectionViews)
            {
                sectionView.UpdateView();
            }
        }

        private void RegisterNavigationScheme()
        {
            if (_navigationPushed)
                return;

            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Green, "Confirm", Confirm),
                new NavigationScheme.Entry(MenuAction.Up, "Up", Up),
                new NavigationScheme.Entry(MenuAction.Down, "Down", Down)
            }, false));


            _navigationPushed = true;
        }

        private void Confirm()
        {
            if (FirstSelectedIndex == null)
            {
                FirstSelectedIndex = HoveredIndex;
            }
            else
            {
                LastSelectedIndex = HoveredIndex;

                int first = FirstSelectedIndex.Value;
                int last = LastSelectedIndex.Value;

                GameManager.PracticeManager.SetPracticeSection(_sections[first], _sections[last]);
                GameManager.Resume(inputCompensation: false);

                // Hide menu
                _pauseMenuManager.PopMenu(resume: false);
            }
        }

        private void Up()
        {
            HoveredIndex--;
        }

        private void Down()
        {
            HoveredIndex++;
        }

        private void Update()
        {
            if (_scrollTimer > 0f)
            {
                _scrollTimer -= Time.deltaTime;
                return;
            }

            var delta = Mouse.current.scroll.ReadValue().y * Time.deltaTime;

            if (delta > 0f)
            {
                HoveredIndex--;
                _scrollTimer = SCROLL_TIME;
                return;
            }

            if (delta < 0f)
            {
                HoveredIndex++;
                _scrollTimer = SCROLL_TIME;
            }
        }
    }
}