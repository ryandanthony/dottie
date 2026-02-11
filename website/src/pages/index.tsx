import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

type FeatureItem = {
  title: string;
  description: ReactNode;
  link: string;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'YAML Configuration',
    description: (
      <>
        Define dotfile symlinks and software installation in a single{' '}
        <code>dottie.yaml</code> file. Simple, readable, version-controlled.
      </>
    ),
    link: '/docs/configuration/overview',
  },
  {
    title: 'Profile Inheritance',
    description: (
      <>
        Create multiple profiles for different contexts—work, home, server—with
        inheritance to avoid duplication.
      </>
    ),
    link: '/docs/configuration/profiles',
  },
  {
    title: 'Install Blocks',
    description: (
      <>
        Install software from APT, GitHub releases, Snap, Nerd Fonts, APT
        repositories, and custom scripts.
      </>
    ),
    link: '/docs/configuration/install-blocks',
  },
  {
    title: 'CLI Commands',
    description: (
      <>
        Simple commands: <code>validate</code>, <code>link</code>,{' '}
        <code>install</code>. Preview changes with <code>--dry-run</code>.
      </>
    ),
    link: '/docs/commands/validate',
  },
];

function Feature({title, description, link}: FeatureItem) {
  return (
    <div className={clsx('col col--6', styles.feature)}>
      <div className="padding-horiz--md padding-vert--md">
        <Heading as="h3">
          <Link to={link}>{title}</Link>
        </Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <div className={styles.cliBadge}>
          <span className={styles.prompt}>&gt;</span> dottie
        </div>
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/getting-started/installation">
            Get Started
          </Link>
        </div>
      </div>
    </header>
  );
}

function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description="A dotfile manager and software installation tool for Linux">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
