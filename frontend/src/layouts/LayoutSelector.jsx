import { useSelector } from "react-redux";
import LandingLayout from "./LandingLayout";
import AuthLayout from "./AuthLayout";
import MainLayout from "./MainLayout";


export default function LayoutSelector({ children }) {
  const current_user = useSelector((state) => state.user);
const isAuthenticated = current_user?.isAuthenticated;

  let LayoutToShow = LandingLayout;
  if (isAuthenticated) LayoutToShow = MainLayout;

  return (
    <div>
      <LayoutToShow />
      <main>{children}</main>
    </div>
  );
}
