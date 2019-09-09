﻿using System.Collections;
using System.Collections.Generic;

using Juniper.Progress;

namespace Juniper.Widgets
{
    public class MenuCollection : SubSceneController
    {
        private readonly Dictionary<string, AbstractStateController> views = new Dictionary<string, AbstractStateController>();

        public string firstView;

        public void OnValidate()
        {
            if (string.IsNullOrEmpty(firstView))
            {
                var views = GetComponentsInChildren<MenuView>(true);
                if (views.Length > 0)
                {

                    firstView = views[0].name;
                }
            }
        }

        public override void Awake()
        {
            base.Awake();

            this.views.Clear();

            var views = GetComponentsInChildren<MenuView>(true);
            foreach (var view in views)
            {
                AddView(view);
            }

            foreach (var view in this.views.Values)
            {
                view.SkipExit();
            }
        }

        private void AddView(MenuView view)
        {
            views.Add(view.name, view.GetComponent<AbstractStateController>());
        }

        public void ShowView(string name)
        {
            StartCoroutine(ShowViewCoroutine(name));
        }

        private IEnumerator ShowViewCoroutine(string name)
        {
            foreach (var view in views)
            {
                if (view.Key != name && !view.Value.IsExited)
                {
                    yield return view.Value.ExitCoroutine();
                }
            }

            if (name != null)
            {
                yield return views[name].EnterCoroutine();
            }
        }

        public override void Enter(IProgress prog = null)
        {
            base.Enter(prog);
            prog?.Report(1);
            Complete();
        }

        protected override void OnEntered()
        {
            base.OnEntered();
            ShowView(firstView);
        }

        protected override void OnExiting()
        {
            base.OnExiting();
            StartCoroutine(ExitingCouroutine());
        }

        private IEnumerator ExitingCouroutine()
        {
            yield return ShowViewCoroutine(null);
            Complete();
        }
    }
}
