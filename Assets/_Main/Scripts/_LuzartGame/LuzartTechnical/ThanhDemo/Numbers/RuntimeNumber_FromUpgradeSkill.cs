using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
namespace Luzart
{
    public class RuntimeNumber_FromUpgradeSkill : RuntimeNumber
    {
        private StatType _statType;
        private IStatDefinition _iStatDefinition;
        private IValue<List<StatCalculator>> _variableStatCalculators;
        private INumber _runtimeNumber_SimpleBoosted;
        private List<StatCalculator> _statCaculatorAdd = new List<StatCalculator>();
        private List<StatCalculator> _statCaculatorMultiply = new();
        private INumber _baseValue = new Number(0);
        private INumberWithSet _baseAddValue = new Number();
        private INumberWithSet _baseMultiplyValue = new Number();
        public RuntimeNumber_FromUpgradeSkill(IStatDefinition iStatDefinition, INumber baseValue, IValue<List<StatCalculator>> variableStatCalculators) 
        {
            this._iStatDefinition = iStatDefinition;
            this._variableStatCalculators = variableStatCalculators;
            this._baseValue = baseValue;
            this._statCaculatorAdd.Clear();
            this._statCaculatorMultiply.Clear();
            variableStatCalculators.Changed += Recalculate;
            Recalculate(variableStatCalculators);
        }
        public RuntimeNumber_FromUpgradeSkill(IStatDefinition iStatDefinition, INumber baseValue, List<StatCalculator> variableStatCalculators)
        {
            this._iStatDefinition = iStatDefinition;
            this._baseValue = baseValue;
            this._statCaculatorAdd.Clear();
            this._statCaculatorMultiply.Clear();
            Recalculate(variableStatCalculators);
        }
        public RuntimeNumber_FromUpgradeSkill(StatType statType, INumber baseValue, IValue<List<StatCalculator>> variableStatCalculators)
        {
            this._statType = statType;
            this._baseValue = baseValue;
            this._statCaculatorAdd.Clear();
            this._statCaculatorMultiply.Clear();
            variableStatCalculators.Changed += Recalculate;
            Recalculate(variableStatCalculators.Value,statType);
        }
        public RuntimeNumber_FromUpgradeSkill(StatType statType, INumber baseValue, List<StatCalculator> variableStatCalculators)
        {
            this._statType = statType;
            this._baseValue = baseValue;
            this._statCaculatorAdd.Clear();
            this._statCaculatorMultiply.Clear();
            Recalculate(variableStatCalculators, statType);
        }
        protected override double DoGetValue()
        {
            return _runtimeNumber_SimpleBoosted.Value;
        }
        private void Recalculate(IValue<List<StatCalculator>> variableStatCalculators)
        {
            Recalculate(variableStatCalculators.Value);
        }
        private void Recalculate(List<StatCalculator> variableStatCalculators)
        {
            int length = variableStatCalculators.Count;
            for (int i = 0; i < length; i++)
            {
                var statCalculator = variableStatCalculators[i];
                if (statCalculator.Definition is IStatDefinition statDefinition)
                {
                    if (statDefinition == _iStatDefinition)
                    {
                        switch (statCalculator.ModeCalculation)
                        {
                            case ModeCalculation.Add:
                                _statCaculatorAdd.Add(statCalculator);
                                break;
                            case ModeCalculation.MultiBase:
                                _statCaculatorAdd.Add(statCalculator);
                                break;
                            case ModeCalculation.MultiAll:
                                _statCaculatorMultiply.Add(statCalculator);
                                break;
                        }
                    }
                }
            }
            double resultAdd = 0;
            foreach (var statCalculator in _statCaculatorAdd)
            {
                double value = GetValueCalculator(statCalculator, resultAdd);
                resultAdd += value;
            }
            _baseAddValue.Set(resultAdd);
            double resultMultiply = 1;
            foreach (var statCalculator in _statCaculatorMultiply)
            {
                double value = GetValueCalculator(statCalculator, resultAdd);
                resultMultiply *= value;
            }
            _baseMultiplyValue.Set(resultMultiply);
            if(_runtimeNumber_SimpleBoosted == null)
            {
                _runtimeNumber_SimpleBoosted = new RuntimeNumber_SimpleBoosted(_baseValue, _baseAddValue, _baseMultiplyValue);
            }
        }
        private void Recalculate(List<StatCalculator> variableStatCalculators, StatType statType)
        {
            int length = variableStatCalculators.Count;
            for (int i = 0; i < length; i++)
            {
                var statCalculator = variableStatCalculators[i];
                if (statCalculator.Definition is IStatDefinition statDefinition)
                {
                    if (statDefinition.StatType == statType)
                    {
                        switch (statCalculator.ModeCalculation)
                        {
                            case ModeCalculation.Add:
                                _statCaculatorAdd.Add(statCalculator);
                                break;
                            case ModeCalculation.MultiBase:
                                _statCaculatorAdd.Add(statCalculator);
                                break;
                            case ModeCalculation.MultiAll:
                                _statCaculatorMultiply.Add(statCalculator);
                                break;
                        }
                    }
                }
            }
            double resultAdd = 0;
            foreach (var statCalculator in _statCaculatorAdd)
            {
                double value = GetValueCalculator(statCalculator, resultAdd);
                resultAdd += value;
            }
            _baseAddValue.Set(resultAdd);
            double resultMultiply = 1;
            foreach (var statCalculator in _statCaculatorMultiply)
            {
                double value = GetValueCalculator(statCalculator, resultAdd);
                resultMultiply *= value;
            }
            _baseMultiplyValue.Set(resultMultiply);
            if (_runtimeNumber_SimpleBoosted == null)
            {
                _runtimeNumber_SimpleBoosted = new RuntimeNumber_SimpleBoosted(_baseValue, _baseAddValue, _baseMultiplyValue);
            }
        }
        private double GetValueCalculator(ICalculator calculator, double baseValue)
        {
            return calculator.Calculate(baseValue);
        }
        protected override void DoDispose()
        {
            base.DoDispose();
            _variableStatCalculators.Changed -= Recalculate;
        }
    }
}
