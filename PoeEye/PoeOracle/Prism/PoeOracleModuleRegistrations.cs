using Microsoft.Practices.Unity;
using PoeOracle.Models;
using PoeOracle.PoeDatabase;
using PoeOracle.ViewModels;
using PoeShared.Scaffolding;

namespace PoeOracle.Prism
{
    internal sealed class PoeOracleModuleRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IExternalUriOpener, ExternalUriOpener>()
                .RegisterSingleton<ISkillGemInfoProvider, SkillGemInfoProvider>();
            
            Container
                .RegisterType<ISuggestionsDataSource>(
                    new InjectionFactory(unity => unity.Resolve<ComplexSuggestionsDataSource>(
                            new DependencyOverride<ISuggestionProvider[]>(
                                    new ISuggestionProvider[]
                                    {
                                        unity.Resolve<GemSuggestionProvider>(),
                                        unity.Resolve<EmptySuggestionProvider>(),
                                    }
                                )
                        ))
                );
        }
    }
}