﻿using Microsoft.Practices.Unity;
using PoeEye.ItemParser.Converters;
using PoeEye.ItemParser.Services;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.ItemParser.Prism
{
    internal sealed class ItemParserRegistrations : UnityContainerExtension
    {
       
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IConverter<IPoeItem, IPoeQueryInfo>, PoeItemToPoeQueryConverter>()
                .RegisterSingleton<IPoeModsProcessor, PoeModsProcessor>()
                .RegisterSingleton<IPoeItemSerializer, PoeItemSerializer>()
                .RegisterSingleton<IPoeItemParser, PoeItemParser>();
        }
    }
}