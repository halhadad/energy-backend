import { useSelector } from "react-redux";
import { Outlet, Navigate } from "react-router";

function PrivateRouter() {
    const current_user = useSelector((state) => state.user);
const isAuthenticated = current_user?.isAuthenticated;

    // EDIT: For testing purpose only
    // return <Outlet />;
    return isAuthenticated ? <Outlet /> : <Navigate to="/signin" />;

    

}
export default PrivateRouter;