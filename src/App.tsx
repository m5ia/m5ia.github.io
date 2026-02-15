import { useEffect, useRef, useState } from 'react';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { 
  Briefcase, 
  Target, 
  TrendingUp, 
  Cpu, 
  ShoppingBag, 
  UtensilsCrossed,
  MapPin,
  ArrowRight,
  Mail,
  Menu,
  X,
  ChevronDown
} from 'lucide-react';
import { Button } from '@/components/ui/button';

gsap.registerPlugin(ScrollTrigger);

const services = [
  {
    title: 'M&A Advisory',
    description: 'End-to-end mergers and acquisitions support, from target identification and due diligence to deal structuring and post-merger integration.',
  },
  {
    title: 'Capital Raising',
    description: 'Strategic guidance on fundraising initiatives, investor relations, and capital structure optimization for growth-stage companies.',
  },
  {
    title: 'Strategic Advisory',
    description: 'Corporate strategy development, market entry planning, and business transformation initiatives that drive sustainable value creation.',
  }
];

const focusAreas = [
  {
    icon: Briefcase,
    title: 'Mergers & Acquisitions',
    description: 'Strategic deal structuring and execution across multiple sectors, from due diligence to post-merger integration.',
  },
  {
    icon: Target,
    title: 'Strategy',
    description: 'Corporate strategy and business transformation initiatives that drive sustainable competitive advantage.',
  },
  {
    icon: TrendingUp,
    title: 'Private Equity',
    description: 'Investment analysis and portfolio management with a focus on value creation and operational excellence.',
  },
  {
    icon: Cpu,
    title: 'Technology & AI',
    description: 'Digital transformation, AI applications in business, and tech-enabled business models.',
  },
  {
    icon: ShoppingBag,
    title: 'Consumer Products',
    description: 'Consumer brands and market trends analysis, identifying growth opportunities in evolving markets.',
  },
  {
    icon: UtensilsCrossed,
    title: 'Food & Beverage',
    description: 'F&B sector investments and innovation, from emerging brands to established market leaders.',
  }
];

function App() {
  const [isNavVisible, setIsNavVisible] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const heroRef = useRef<HTMLDivElement>(null);
  const servicesRef = useRef<HTMLDivElement>(null);
  const focusRef = useRef<HTMLDivElement>(null);
  const contactRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const heroTl = gsap.timeline({ delay: 0.3 });
    
    heroTl.from('.hero-badge', {
      opacity: 0,
      y: 20,
      duration: 0.6,
      ease: 'power3.out'
    })
    .from('.hero-name', {
      opacity: 0,
      y: 40,
      duration: 1,
      ease: 'power3.out'
    }, '-=0.4')
    .from('.hero-title', {
      opacity: 0,
      y: 30,
      duration: 0.8,
      ease: 'power3.out'
    }, '-=0.6')
    .from('.hero-location', {
      opacity: 0,
      y: 20,
      duration: 0.6,
      ease: 'power3.out'
    }, '-=0.4')
    .from('.hero-description', {
      opacity: 0,
      y: 30,
      duration: 0.8,
      ease: 'power3.out'
    }, '-=0.3')
    .from('.hero-buttons', {
      opacity: 0,
      y: 20,
      duration: 0.6,
      ease: 'power3.out'
    }, '-=0.4')
    .from('.scroll-indicator', {
      opacity: 0,
      duration: 0.6,
      ease: 'power2.out'
    }, '-=0.2');

    gsap.to('.scroll-indicator', {
      y: 10,
      duration: 1,
      repeat: -1,
      yoyo: true,
      ease: 'power1.inOut'
    });

    ScrollTrigger.create({
      trigger: heroRef.current,
      start: 'bottom top+=100',
      onEnter: () => setIsNavVisible(true),
      onLeaveBack: () => setIsNavVisible(false)
    });

    gsap.from('.service-card', {
      scrollTrigger: {
        trigger: servicesRef.current,
        start: 'top 75%',
        toggleActions: 'play none none reverse'
      },
      y: 40,
      duration: 0.6,
      stagger: 0.1,
      ease: 'power3.out'
    });

    gsap.from('.focus-card', {
      scrollTrigger: {
        trigger: focusRef.current,
        start: 'top 75%',
        toggleActions: 'play none none reverse'
      },
      y: 40,
      duration: 0.6,
      stagger: 0.08,
      ease: 'power3.out'
    });

    gsap.from('.contact-content', {
      scrollTrigger: {
        trigger: contactRef.current,
        start: 'top 75%',
        toggleActions: 'play none none reverse'
      },
      y: 40,
      duration: 0.8,
      ease: 'power3.out'
    });

    return () => {
      ScrollTrigger.getAll().forEach(trigger => trigger.kill());
    };
  }, []);

  const scrollToSection = (ref: React.RefObject<HTMLDivElement | null>) => {
    ref.current?.scrollIntoView({ behavior: 'smooth' });
    setMobileMenuOpen(false);
  };

  return (
    <div className="min-h-screen bg-background relative">
      {/* Navigation */}
      <nav 
        className={`fixed top-0 left-0 right-0 z-50 transition-all duration-500 ${
          isNavVisible 
            ? 'opacity-100 translate-y-0' 
            : 'opacity-0 -translate-y-full pointer-events-none'
        }`}
      >
        <div className="glass border-b border-white/5">
          <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
            <a 
              href="#" 
              className="text-xl font-bold gradient-text"
              onClick={(e) => {
                e.preventDefault();
                window.scrollTo({ top: 0, behavior: 'smooth' });
              }}
            >
              m5ia
            </a>
            
            <div className="hidden md:flex items-center gap-8">
              <button 
                onClick={() => scrollToSection(servicesRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Services
              </button>
              <button 
                onClick={() => scrollToSection(focusRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Focus Areas
              </button>
              <button 
                onClick={() => scrollToSection(contactRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Contact
              </button>
              <Button 
                onClick={() => scrollToSection(contactRef)}
                className="bg-primary text-primary-foreground hover:bg-primary/90 btn-shine"
              >
                Let's Talk
              </Button>
            </div>

            <button 
              className="md:hidden text-foreground"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              {mobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
            </button>
          </div>

          {mobileMenuOpen && (
            <div className="md:hidden glass border-t border-white/5 px-6 py-4">
              <div className="flex flex-col gap-4">
                <button 
                  onClick={() => scrollToSection(servicesRef)}
                  className="text-left text-muted-foreground hover:text-foreground transition-colors py-2"
                >
                  Services
                </button>
                <button 
                  onClick={() => scrollToSection(focusRef)}
                  className="text-left text-muted-foreground hover:text-foreground transition-colors py-2"
                >
                  Focus Areas
                </button>
                <button 
                  onClick={() => scrollToSection(contactRef)}
                  className="text-left text-muted-foreground hover:text-foreground transition-colors py-2"
                >
                  Contact
                </button>
              </div>
            </div>
          )}
        </div>
      </nav>

      {/* Hero Section */}
      <section 
        ref={heroRef}
        className="min-h-screen flex items-center justify-center relative overflow-hidden grid-bg"
      >
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-primary/10 rounded-full blur-[120px] pointer-events-none" />
        <div className="absolute bottom-1/4 right-1/4 w-80 h-80 bg-primary/5 rounded-full blur-[100px] pointer-events-none" />
        
        <div className="max-w-5xl mx-auto px-6 py-20 text-center relative z-10">
          {/* Friendly greeting with waving hand */}
          <div className="hero-badge mb-10">
            <span className="text-4xl md:text-5xl waving-hand inline-block">👋</span>
          </div>
          
          <h1 className="hero-name text-5xl md:text-7xl lg:text-8xl font-bold mb-6 tracking-tight">
            <span className="text-foreground">Investment</span>
            <span className="gradient-text"> Professional</span>
            <br />
            <span className="text-foreground">& Tech</span>
            <span className="gradient-text"> Enthusiast</span>
          </h1>
          
          <div className="hero-location flex items-center justify-center gap-2 text-muted-foreground mb-8">
            <MapPin size={18} className="text-primary" />
            <span className="text-lg">London-based M&A and private equity specialist</span>
          </div>
          
          <p className="hero-description text-xl md:text-2xl text-muted-foreground max-w-3xl mx-auto mb-12 leading-relaxed">
            An investment and finance professional with a technical edge, specialising in mergers & acquisitions 
            and private equity. Exploring new ventures at the intersection of finance, technology, 
            F&B, and consumer markets.
          </p>
          
          <div className="hero-buttons flex flex-col sm:flex-row items-center justify-center gap-4">
            <Button 
              size="lg"
              onClick={() => scrollToSection(contactRef)}
              className="bg-primary text-primary-foreground hover:bg-primary/90 btn-shine group px-8 py-6 text-lg"
            >
              Let's Talk
              <ArrowRight className="ml-2 group-hover:translate-x-1 transition-transform" size={20} />
            </Button>
            <Button 
              size="lg"
              variant="outline"
              onClick={() => scrollToSection(servicesRef)}
              className="border-white/20 hover:bg-white/5 px-8 py-6 text-lg"
            >
              View Services
            </Button>
          </div>
        </div>

        <div className="scroll-indicator absolute bottom-8 left-1/2 -translate-x-1/2 flex flex-col items-center gap-2 text-muted-foreground">
          <span className="text-sm">Scroll to explore</span>
          <ChevronDown size={20} className="text-primary" />
        </div>
      </section>

      {/* Services Section - Main Selling Point */}
      <section ref={servicesRef} className="py-24 md:py-32 relative">
        <div className="max-w-6xl mx-auto px-6">
          <div className="text-center mb-16">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full glass mb-6">
              <span className="w-1.5 h-1.5 bg-primary rounded-full" />
              <span className="text-xs text-muted-foreground uppercase tracking-wider">What I Offer</span>
            </div>
            <h2 className="text-4xl md:text-5xl font-bold mb-6">
              Advisory <span className="gradient-text">Services</span>
            </h2>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Providing strategic guidance to businesses and investors across the full deal lifecycle.
            </p>
          </div>
          
          <div className="grid md:grid-cols-3 gap-6">
            {services.map((service, index) => (
              <div 
                key={index}
                className="service-card group relative glass rounded-2xl p-8 card-hover overflow-hidden border border-white/5"
              >
                <div className="absolute inset-0 bg-gradient-to-br from-primary/10 via-transparent to-primary/5 opacity-0 group-hover:opacity-100 transition-opacity duration-500" />
                
                <div className="relative z-10">
                  <div className="w-12 h-12 rounded-xl bg-primary/20 flex items-center justify-center mb-6 group-hover:scale-110 transition-transform duration-300">
                    <span className="text-2xl font-bold text-primary">{index + 1}</span>
                  </div>
                  
                  <h3 className="text-xl font-semibold mb-4">{service.title}</h3>
                  <p className="text-muted-foreground leading-relaxed">{service.description}</p>
                </div>
              </div>
            ))}
          </div>
          
          {/* Stats row */}
          <div className="mt-16 grid grid-cols-1 md:grid-cols-3 gap-6 max-w-3xl mx-auto">
            <div className="glass rounded-xl p-6 text-center">
              <p className="text-3xl md:text-4xl font-bold gradient-text mb-2">10+</p>
              <p className="text-sm text-muted-foreground">Years Experience</p>
            </div>
            <div className="glass rounded-xl p-6 text-center">
              <p className="text-3xl md:text-4xl font-bold gradient-text mb-2">25+</p>
              <p className="text-sm text-muted-foreground">Deals Closed</p>
            </div>
            <div className="glass rounded-xl p-6 text-center">
              <p className="text-3xl md:text-4xl font-bold gradient-text mb-2">£10bn+</p>
              <p className="text-sm text-muted-foreground">Transaction Value</p>
            </div>
          </div>
        </div>
      </section>

      {/* Focus Areas Section */}
      <section ref={focusRef} className="py-24 md:py-32 relative">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-16">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full glass mb-6">
              <span className="w-1.5 h-1.5 bg-primary rounded-full" />
              <span className="text-xs text-muted-foreground uppercase tracking-wider">Expertise</span>
            </div>
            <h2 className="text-4xl md:text-5xl font-bold mb-6">
              Focus <span className="gradient-text">Areas</span>
            </h2>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Deep expertise across multiple sectors, bringing industry knowledge to every engagement.
            </p>
          </div>
          
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
            {focusAreas.map((area, index) => (
              <div 
                key={index}
                className="focus-card group relative glass rounded-2xl p-6 card-hover overflow-hidden border border-white/5"
              >
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-primary/5 opacity-0 group-hover:opacity-100 transition-opacity duration-500" />
                
                <div className="relative z-10">
                  <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center mb-5 group-hover:scale-110 transition-transform duration-300">
                    <area.icon size={22} className="text-primary" />
                  </div>
                  
                  <h3 className="text-lg font-semibold mb-3">{area.title}</h3>
                  <p className="text-muted-foreground text-sm leading-relaxed">{area.description}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Contact Section */}
      <section ref={contactRef} className="py-24 md:py-32 relative overflow-hidden">
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-primary/5 rounded-full blur-[150px] pointer-events-none" />
        
        <div className="max-w-4xl mx-auto px-6 relative z-10">
          <div className="contact-content glass rounded-3xl p-8 md:p-16 text-center border border-white/5">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 mb-8">
              <span className="w-1.5 h-1.5 bg-primary rounded-full animate-pulse" />
              <span className="text-xs text-primary uppercase tracking-wider">Open for opportunities</span>
            </div>
            
            <h2 className="text-4xl md:text-5xl font-bold mb-6">
              Let's <span className="gradient-text">Connect</span>
            </h2>
            
            <p className="text-xl text-muted-foreground mb-10 max-w-2xl mx-auto">
              Always open to interesting conversations about finance, technology, 
              and new opportunities. Whether it's a potential collaboration, advisory engagement, or just 
              a coffee chat, I'd love to hear from you.
            </p>
            
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-12">
              <Button 
                size="lg"
                className="bg-primary text-primary-foreground hover:bg-primary/90 btn-shine group px-8 py-6 text-lg w-full sm:w-auto"
                onClick={() => window.open('mailto:max@m5ia.com', '_blank')}
              >
                <Mail className="mr-2" size={20} />
                Get In Touch
                <ArrowRight className="ml-2 group-hover:translate-x-1 transition-transform" size={20} />
              </Button>
            </div>
            

          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-8 border-t border-white/5">
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex flex-col md:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <span className="text-xl font-bold gradient-text">m5ia</span>
            </div>
            
            <p className="text-sm text-muted-foreground">
              © {new Date().getFullYear()} m5ia.com. All rights reserved.
            </p>
            
            <div className="flex items-center gap-6">
              <button 
                onClick={() => scrollToSection(servicesRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Services
              </button>
              <button 
                onClick={() => scrollToSection(focusRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Focus Areas
              </button>
              <button 
                onClick={() => scrollToSection(contactRef)}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                Contact
              </button>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}

export default App;
