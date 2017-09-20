﻿using DnDGen.Core.Mappers.Percentiles;
using RollGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDGen.Core.Selectors.Percentiles
{
    internal class PercentileSelector : IPercentileSelector
    {
        private readonly PercentileMapper percentileMapper;
        private readonly Dice dice;

        public PercentileSelector(PercentileMapper percentileMapper, Dice dice)
        {
            this.percentileMapper = percentileMapper;
            this.dice = dice;
        }

        public string SelectFrom(string tableName)
        {
            return SelectFrom<string>(tableName);
        }

        public IEnumerable<string> SelectAllFrom(string tableName)
        {
            return SelectAllFrom<string>(tableName);
        }

        public T SelectFrom<T>(string tableName)
        {
            var table = percentileMapper.Map(tableName);
            var roll = dice.Roll().Percentile().AsSum();

            if (!table.ContainsKey(roll))
            {
                throw new ArgumentException($"{roll} is not a valid entry in the table {tableName}");
            }

            return GetValue<T>(table[roll]);
        }

        private T GetValue<T>(object source)
        {
            return (T)Convert.ChangeType(source, typeof(T));
        }

        public IEnumerable<T> SelectAllFrom<T>(string tableName)
        {
            var table = percentileMapper.Map(tableName);
            return table.Values.Select(v => GetValue<T>(v)).Distinct();
        }

        public bool SelectFrom(double chance)
        {
            if (chance >= 1)
                return true;
            else if (chance <= 0)
                return false;

            var result = dice.Roll().Percentile().AsSum() / 100d;
            return result <= chance;
        }
    }
}