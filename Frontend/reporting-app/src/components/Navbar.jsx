import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ShieldAlert, LogIn, UserPlus } from 'lucide-react';
import './Navbar.css';

const Navbar = () => {
  const location = useLocation();
  const isAuthPage = location.pathname === '/login' || location.pathname === '/signup';

  return (
    <nav className="navbar glass-nav animate-fade-in">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          <div className="logo-icon-wrapper">
            <ShieldAlert size={28} className="logo-icon" />
          </div>
          <span className="logo-text">SecureReport</span>
        </Link>
        
        {!isAuthPage && (
          <div className="navbar-actions">
            <Link to="/login" className="btn btn-ghost">
              <LogIn size={18} />
              <span>Sign In</span>
            </Link>
            <Link to="/login?mode=signup" className="btn btn-primary">
              <UserPlus size={18} />
              <span>Get Started</span>
            </Link>
          </div>
        )}
      </div>
    </nav>
  );
};

export default Navbar;
