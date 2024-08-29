﻿using Positron.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Positron.Resources;

namespace Positron.Controls
{

    public partial class NativeWebView : WebView
    {

        static NativeWebView() {
            OnStaticPlatformInit();
        }

        private DisposableList disposables = new DisposableList();

        public IJSContext Context { get;set; }

        partial void OnPlatformInit();

        static  partial void OnStaticPlatformInit();

        public GlobalClr Clr { get; }

        public NativeWebView()
        {
            Context = JSContextFactory.Instance.Create();
            this.Clr = new GlobalClr();

            // Need to invoke TSLib in the global context...
            Context.Evaluate(Scripts.TSLib, "tslib.js");

            Context["clr"] = Context.Marshal(Clr);

            Context["serialize"] = Context.CreateFunction(1, (c, s) => {
                try
                {
                    var arg0 = s[0];
                    var serialized = Clr.Serialize(arg0);
                    return Context.CreateString(serialized);
                } catch (Exception error)
                {
                    System.Diagnostics.Debug.WriteLine(error.ToString());
                    return Context.CreateString("null");
                }
            }, "serialize");

            Context["evalInPage"] = Context.CreateFunction(1, (c, s) => {
                var script = s[0].ToString();
                this.Eval(script);
                return Context.Undefined;
            }, "sendToBrowser");

            this.VerticalOptions = LayoutOptions.Fill;
            this.HorizontalOptions = LayoutOptions.Fill;

            // setup channel...
            OnPlatformInit();

            Positron.Instance.OnUrlRequested += Instance_OnUrlRequested;
            Positron.Instance.OnDeviceTokenUpdated+= Instance_OnDeviceTokenUpdated;

        }


        private void Instance_OnUrlRequested(object? sender, EventArgs e)
        {
            try
            {
                this.Eval("document.body.dispatchEvent(new CustomEven('urlRequested', { details: {}, bubbles: true });");
            }
            catch
            { }
        }

        private void Instance_OnDeviceTokenUpdated(object? sender, EventArgs e)
        {
            try
            {
                this.Eval("document.body.dispatchEvent(new CustomEven('deviceTokenUpdated', { details: {}, bubbles: true });");
            } catch
            {}
        }

        /// <summary>
        /// This JavaScript will have access to entire CLR and will be able to execute
        /// everything in CLR.
        /// </summary>
        /// <param name="script"></param>
        /// <param name="callback"></param>
        public void RunMainThreadJavaScript(string script)
        {
            Dispatcher.DispatchTask( async () => {
                try
                {
                    var result = await this.Clr.SerializeAsync(Context.Evaluate(script));
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            });
        }

        ~NativeWebView()
        {
            disposables.Dispose();
        }


    }
}