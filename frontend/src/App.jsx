import { BrowserRouter, Route, Routes } from "react-router";
import { Provider } from "react-redux";
import { PersistGate } from "redux-persist/integration/react";
import { Toaster } from "react-hot-toast";
import { store, persistor } from "./redux/store";
import { useGlobalAlerts } from "./hooks/useGlobalAlerts";
import { useOrganisations } from "./hooks/useOrganisations";
import Overview from "./pages/Overview.jsx";
import Signin from "./pages/Signin.jsx";
import Signup from "./pages/Signup.jsx";
import Organisations from "./pages/Organisations.jsx";
import Devices from "./pages/Devices.jsx";
import Alerts from "./pages/Alerts.jsx";
import Profile from "./pages/Profile.jsx";
import Settings from "./pages/Settings.jsx";
import Analytics from "./pages/Analytics.jsx";
import PrivateRouter from "./routers/PrivateRouter.jsx";
import LayoutSelector from "./layouts/LayoutSelector.jsx";

function GlobalListeners() {
  useGlobalAlerts();
  useOrganisations();
  return null;
}

function App() {
  return (
    <Provider store={store}>
      <Toaster position="top-right" />
      <PersistGate loading={null} persistor={persistor}>
        <GlobalListeners />
        <BrowserRouter>
          <Routes>
            <Route element={<LayoutSelector />}>
              <Route path="/" element={<Overview />} />
              <Route path="/signin" element={<Signin />} />
              <Route path="/signup" element={<Signup />} />
              <Route element={<PrivateRouter />}>
                <Route path="/organisations" element={<Organisations />} />
                <Route path="/organisations/devices/:orgId" element={<Devices />} />
                <Route path="/alerts" element={<Alerts />} />
                <Route path="/profile" element={<Profile />} />
                <Route path="/settings" element={<Settings />} />
                <Route path="/analytics" element={<Analytics />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </PersistGate>
    </Provider>
  );
}
export default App;