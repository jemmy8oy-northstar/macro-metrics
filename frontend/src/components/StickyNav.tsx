import "./StickyNav.css";

export function StickyNav() {
  const scrollTo = (id: string) =>
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth" });

  return (
    <nav className="sticky-nav">
      <button onClick={() => scrollTo("presets")}>Presets</button>
      <button onClick={() => scrollTo("compare")}>Compare</button>
      <button onClick={() => scrollTo("indicators")}>Indicators</button>
    </nav>
  );
}
