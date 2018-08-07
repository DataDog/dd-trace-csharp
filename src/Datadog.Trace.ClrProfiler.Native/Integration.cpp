#include "Integration.h"

const integration asp_net_mvc5_integration
    = ::integration(
                    // integration_type
                    IntegrationType_AspNet_Mvc5,
                    // integration_name
                    L"aspNetMvc5",
                    // target_assembly_name
                    L"System.Web.Mvc",
                    // wrapper_assembly_name
                    L"Datadog.Trace.ClrProfiler.Managed",
                    // wrapper_type_name
                    L"Datadog.Trace.ClrProfiler.Integrations.AspNetMvc5Integration",
                    // method_replacements
                    {
                        method_replacement(
                                           // caller_method_token: [System.Web.Mvc]System.Web.Mvc.Controller.BeginExecuteCore() ...
                                           0x06000FA5,
                                           // target_method_token: [System.Web.Mvc]System.Web.Mvc.Async.IAsyncActionInvoker.BeginInvokeAction()
                                           0x06000CA1,
                                           // wrapper_method_name
                                           L"BeginInvokeAction",
                                           // wrapper_method_signature
                                           {
                                               // calling convention
                                               IMAGE_CEE_CS_CALLCONV_DEFAULT,
                                               // parameter count
                                               0x05,
                                               // return type
                                               ELEMENT_TYPE_OBJECT,
                                               // parameter types
                                               ELEMENT_TYPE_OBJECT,
                                               ELEMENT_TYPE_OBJECT,
                                               ELEMENT_TYPE_OBJECT,
                                               ELEMENT_TYPE_OBJECT,
                                               ELEMENT_TYPE_OBJECT,
                                           }),
                        method_replacement(
                                           // caller_method_token: [System.Web.Mvc]System.Web.Mvc.Controller.BeginExecuteCore() ...
                                           0x06000fA6,
                                           // target_method_token: [System.Web.Mvc]System.Web.Mvc.Async.IAsyncActionInvoker.EndInvokeAction()
                                           0x06000CA2,
                                           // wrapper_method_name
                                           L"EndInvokeAction",
                                           // wrapper_method_signature
                                           {
                                               // calling convention
                                               IMAGE_CEE_CS_CALLCONV_DEFAULT,
                                               // parameter count
                                               0x02,
                                               // return type
                                               ELEMENT_TYPE_BOOLEAN,
                                               // parameter types
                                               ELEMENT_TYPE_OBJECT,
                                               ELEMENT_TYPE_OBJECT,
                                           })
                    });

const std::vector<integration> all_integrations = {
    asp_net_mvc5_integration,
};