import { CompareProvider } from "../context/CompareContext";
import { StickyNav } from "../components/StickyNav";
import { PresetCard } from "../components/PresetCard";
import { IndicatorCard } from "../components/IndicatorCard";
import { CompareSection } from "../components/CompareSection";
import { PRESETS } from "../data/presets";
import { INDICATORS } from "../data/indicators";
import "./Home.css";

const Home = () => (
  <CompareProvider>
    <StickyNav />
    <main className="home">
      <section className="home__section" id="presets">
        <h2 className="home__section-title">Preset Ratios</h2>
        <div className="home__preset-grid">
          {PRESETS.map((preset) => (
            <PresetCard key={preset.id} preset={preset} />
          ))}
        </div>
      </section>

      <section className="home__section" id="compare">
        <h2 className="home__section-title">Custom Comparison</h2>
        <CompareSection />
      </section>

      <section className="home__section" id="indicators">
        <h2 className="home__section-title">Macro Indicators</h2>
        <div className="home__indicators-row">
          {INDICATORS.map((ind) => (
            <IndicatorCard key={ind.id} indicator={ind} />
          ))}
        </div>
      </section>
    </main>
  </CompareProvider>
);

export default Home;
