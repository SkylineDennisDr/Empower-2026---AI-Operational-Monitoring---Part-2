namespace Elements
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;

	internal class ElementInstaller
	{
		private readonly IEngine engine;

		public ElementInstaller(IEngine engine)
		{
			this.engine = engine;
		}

		public void InstallDefaultContent()
		{
			int viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Behavioral Anomaly Detection Demo" });
			CreateElement("Empower 2026 - AI - Audio bit rate", "Empower 2026 - AI - Audio bit rate CBR-VBR - fast", "0.0.0.1", viewID);
			CreateElement("Empower 2026 - AI - Task Manager", "Empower 2026 - AI - Task Manager", "0.0.0.1", viewID);

			viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Pattern Matching Demo" });
			CreateElement("Empower 2026 - AI - Video server 1 ", "Empower 2026 - AI - Video Server - fast", "0.0.0.1", viewID);
			CreateElement("Empower 2026 - AI - Video server 2 ", "Empower 2026 - AI - Video Server - fast", "0.0.0.1", viewID);

			viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Proactive Detection Demo" });
			CreateElement("Empower 2026 - AI - SFP Monitor", "Empower 2026 - AI - SFP - fast", "0.0.0.1", viewID);
			CreateElement("Empower 2026 - AI - AMS Server", "Empower 2026 - AI - AMS Server", "0.0.0.3", viewID);

			int CMSViewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026","AI Operational Monitoring", "Proactive Detection Demo",
				"Content Management Servers" });
			CreateElement("Empower 2026 - AI - CMS 1", "Empower 2026 - AI - Content Management Server 1 - fast", "0.0.0.1", CMSViewID);
			CreateElement("Empower 2026 - AI - CMS 2", "Empower 2026 - AI - Content Management Server 2 - fast", "0.0.0.1", CMSViewID);
			CreateElement("Empower 2026 - AI - CMS 3", "Empower 2026 - AI - Content Management Server 3 - fast", "0.0.0.1", CMSViewID);
			AssignVisioToView(CMSViewID, "Empower 2025 - AI - Content Management Server.vsdx");

			System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));
			engine.FindElement("Empower 2026 - AI - Audio bit rate")?.SetParameter(102, 1);
			engine.FindElement("Empower 2026 - AI - CMS 1")?.SetParameter(102, 1);
			engine.FindElement("Empower 2026 - AI - CMS 2")?.SetParameter(102, 1);
			engine.FindElement("Empower 2026 - AI - CMS 3")?.SetParameter(102, 1);

			viewID = CreateViews(new string[] { "DataMiner Catalog", "Empower 2026", "AI Operational Monitoring", "Relational Anomaly Detection Demo" });
			CreateElement($"RAD - Commtia LON 1", "AI - Commtia DAB - fast", "1.0.0.3", viewID, "TrendTemplate_PA_Demo", "AlarmTemplate_PA_Demo");
			Thread.Sleep(5000);
			CreateElement($"RAD - Commtia LON 2", "AI - Commtia DAB - fast", "1.0.0.3", viewID, "TrendTemplate_PA_Demo", "AlarmTemplate_PA_Demo");
			Thread.Sleep(5000);
		}

		private void AssignVisioToView(int viewID, string visioFileName)
		{
			var request = new AssignVisualToViewRequestMessage(viewID, new Skyline.DataMiner.Net.VisualID(visioFileName));

			engine.SendSLNetMessage(request);
		}

		private int? GetView(string viewName)
		{
			var views = engine.SendSLNetMessage(new GetInfoMessage(InfoType.ViewInfo));
			foreach (var m in views)
			{
				var viewInfo = m as ViewInfoEventMessage;
				if (viewInfo == null)
					continue;

				if (viewInfo.Name == viewName)
					return viewInfo.ID;
			}

			return null;
		}

		private int CreateNewView(string viewName, string parentViewName)
		{
			var request = new SetDataMinerInfoMessage
			{
				bInfo1 = int.MaxValue,
				bInfo2 = int.MaxValue,
				DataMinerID = -1,
				HostingDataMinerID = -1,
				IInfo1 = int.MaxValue,
				IInfo2 = int.MaxValue,
				Sa1 = new SA(new string[] { viewName, parentViewName }),
				What = (int)NotifyType.NT_ADD_VIEW_PARENT_AS_NAME,
			};

			var response = engine.SendSLNetSingleResponseMessage(request);
			if (!(response is SetDataMinerInfoResponseMessage infoResponse))
				throw new ArgumentException("Unexpected message returned by DataMiner");

			return infoResponse.iRet;
		}

		private int CreateViews(string[] viewNames)
		{
			int? firstNonExistingViewLevel = null;
			int? lastExistingViewID = null;
			string lastExistingViewName = null;

			for (int i = viewNames.Length - 1; i >= 0; --i)
			{
				int? viewID = GetView(viewNames[i]);
				if (viewID.HasValue)
				{
					lastExistingViewID = viewID;
					lastExistingViewName = viewNames[i];
					firstNonExistingViewLevel = i + 1;
					break;
				}
			}

			if (firstNonExistingViewLevel.HasValue && firstNonExistingViewLevel == viewNames.Length)
				return lastExistingViewID.Value;

			if (!firstNonExistingViewLevel.HasValue)
			{
				// No views in the tree already exist, so create all views starting from the root view
				lastExistingViewID = -1;
				lastExistingViewName = engine.GetDms().GetView(-1).Name;
				firstNonExistingViewLevel = 0;
			}

			for (int i = firstNonExistingViewLevel.Value; i < viewNames.Length; ++i)
			{
				lastExistingViewID = CreateNewView(viewNames[i], lastExistingViewName);
				lastExistingViewName = viewNames[i];
			}

			return lastExistingViewID.Value;
		}

		private void CreateElement(string elementName, string protocolName, string protocolVersion, int viewID,
			string trendTemplate = "Default", string alarmTemplate = "")
		{
			var request = new AddElementMessage
			{
				ElementName = elementName,
				ProtocolName = protocolName,
				ProtocolVersion = protocolVersion,
				TrendTemplate = trendTemplate,
				AlarmTemplate = alarmTemplate,
				ViewIDs = new int[] { viewID },
			};

			var dms = engine.GetDms();
			if (dms.ElementExists(elementName))
				return;

			engine.SendSLNetSingleResponseMessage(request);
		}
	}
}
