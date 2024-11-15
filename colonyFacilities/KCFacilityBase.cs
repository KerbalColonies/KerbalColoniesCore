namespace KerbalColonies.colonyFacilities
{

    /// <summary>
    /// The KCFaciltiyBase class is used to create custom KCFacilities, you must register your types in the typeregistry at startup with the AWAKE method.
    /// If you have custom fields that you want to save overwrite the encode and decode string methods and save the values of your custom fields in the facilityData string.
    /// You can encode the data however you want but you are not allowed to use "{", "}", ",", ":", "=" and "//"
    /// This is because the datastring is saved as a value in a KSP confignode and using these symbols can mess up the loading
    /// I recommend using "|" as seperator and "&" instead of "="
    /// It's NOT checked if the datastring contains any of these symbols!
    /// </summary>
    [System.Serializable]
    public abstract class KCFacilityBase
    {
        public string facilityName;
        public bool enabled;
        public double lastUpdateTime;
        public double creationTime;
        public string facilityData;

        /// <summary>
        /// This function get automatically called, do not call it manually.
        /// The OnBuildingClicked method function should be used for custom windows.
        /// </summary>
        virtual internal void Update()
        {
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        /// <summary>
        /// This function gets automatically called when the building is clicked, it might get used for custom windows.
        /// The update function will be called BEFORE this one, you don't need to do it manually
        /// </summary>
        virtual internal void OnBuildingClicked() { }

        public static void OnBuildingClickedHandler(KerbalKonstructs.Core.StaticInstance instance)
        {
            if (Configuration.coloniesPerBody.ContainsKey(Configuration.gameNode.name))
            {
                if (Configuration.coloniesPerBody[Configuration.gameNode.name].ContainsKey(FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)))
                {
                    if (Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)].ContainsKey(instance.Group))
                    {
                        if (Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][instance.Group].ContainsKey(instance.UUID))
                        {
                            foreach (KCFacilityBase kcFacility in Configuration.coloniesPerBody[Configuration.gameNode.name][FlightGlobals.Bodies.IndexOf(FlightGlobals.currentMainBody)][instance.Group][instance.UUID])
                            {
                                kcFacility.Update();
                                kcFacility.OnBuildingClicked();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method should create the facilityData string from custom fields in derived classes, this base method returns an empty string.
        /// </summary>
        virtual internal string EncodeString()
        {
            facilityData = "";
            return "";
        }

        /// <summary>
        /// This method should fill the custom fields of derived classes, this base method DOES NOTHING.
        /// </summary>
        virtual internal void DecodeString(string facilityData) { }


        protected KCFacilityBase(string facilityName, bool enabled, string facilityData)
        {
            this.facilityName = facilityName;
            this.enabled = enabled;
            lastUpdateTime = Planetarium.GetUniversalTime();
            creationTime = Planetarium.GetUniversalTime();
        }
    }
}
