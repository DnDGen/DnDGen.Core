﻿using DnDGen.Infrastructure.Models;
using DnDGen.Infrastructure.Selectors.Collections;
using DnDGen.Infrastructure.Selectors.Percentiles;
using Ninject.Modules;

namespace DnDGen.Infrastructure.IoC.Modules
{
    internal class SelectorsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IPercentileSelector>().To<PercentileSelector>();
            Bind<ICollectionSelector>().To<CollectionSelector>();

            Kernel.BindDataSelection<TypeAndAmountDataSelection>();
            Bind<IPercentileTypeAndAmountSelector>().To<PercentileTypeAndAmountSelector>();
            Bind<ICollectionTypeAndAmountSelector>().To<CollectionTypeAndAmountSelector>();
        }
    }
}