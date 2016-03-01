﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Internals
{
    internal class Experiment<T> : IExperiment<T>, IExperimentAsync<T>
    {
        readonly static Func<Task<bool>> _alwaysRun = () => Task.FromResult(true);

        string _name;
        Func<Task<T>> _control;
        Func<Task<T>> _candidate;
        Func<T, T, bool> _comparison = DefaultComparison;
        Func<Task> _beforeRun;
        Func<Task<bool>> _runIf = _alwaysRun;
        HashSet<Func<Task<bool>>> _ignores { get; set; } = new HashSet<Func<Task<bool>>>();

        public Experiment(string name)
        {
            _name = name;
        }

        public void RunIf(Func<Task<bool>> block) =>
            _runIf = block;
        public void RunIf(Func<bool> block) =>
            _runIf = () => Task.FromResult(block());

        public void Use(Func<Task<T>> control) =>
            _control = control;

        public void Use(Func<T> control) =>
            _control = () => Task.FromResult(control());

        // TODO add optional name parameter, and store all delegates into a dictionary.

        public void Try(Func<Task<T>> candidate) =>
            _candidate = candidate;

        public void Try(Func<T> candidate) =>
            _candidate = () => Task.FromResult(candidate());

        public void Ignore(Func<bool> block) => 
            _ignores.Add(() => Task.FromResult(block()));

        public void Ignore(Func<Task<bool>> block) => 
            _ignores.Add(block);

        internal ExperimentInstance<T> Build() =>
            new ExperimentInstance<T>(_name, _control, _candidate, _comparison, _beforeRun, _runIf, _ignores);

        public void Compare(Func<T, T, bool> comparison)
        {
            _comparison = comparison;
        }

        static readonly Func<T, T, bool> DefaultComparison = (instance, comparand) =>
        {
            return (instance == null && comparand == null)
                || (instance != null && instance.Equals(comparand))
                || (CompareInstances(instance as IEquatable<T>, comparand));
        };

        static bool CompareInstances(IEquatable<T> instance, T comparand) => instance != null && instance.Equals(comparand);

        public void BeforeRun(Action action)
        {
            _beforeRun = async () => { action(); await Task.FromResult(0); };
        }

        public void BeforeRun(Func<Task> action)
        {
            _beforeRun = action;
        }
    }
}
