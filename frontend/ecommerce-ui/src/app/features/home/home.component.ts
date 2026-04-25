// src/app/features/home/home.component.ts
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/Auth/auth.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  readonly authService = inject(AuthService);

  categories = [
    { icon: '📱', label: 'Electronics',  count: '12,400+' },
    { icon: '👗', label: 'Fashion',       count: '38,200+' },
    { icon: '🏠', label: 'Home & Living', count: '9,800+'  },
    { icon: '💄', label: 'Beauty',        count: '6,300+'  },
    { icon: '⚽', label: 'Sports',        count: '7,100+'  },
    { icon: '📚', label: 'Books',         count: '22,000+' },
    { icon: '🧸', label: 'Toys & Kids',   count: '4,500+'  },
    { icon: '🛒', label: 'Groceries',     count: '3,200+'  },
  ];

  featuredProducts = [
    { name: 'Wireless Pro Headphones',  price: 2999,  originalPrice: 4999,  rating: 4.8, reviews: 1240, badge: 'Best Seller', image: '🎧' },
    { name: 'Smart Watch Series X',     price: 8499,  originalPrice: 11999, rating: 4.6, reviews: 876,  badge: 'New',         image: '⌚' },
    { name: 'Ultra Slim Laptop 14"',    price: 54999, originalPrice: 64999, rating: 4.9, reviews: 432,  badge: 'Top Rated',   image: '💻' },
    { name: 'Mechanical Keyboard RGB',  price: 3499,  originalPrice: 4999,  rating: 4.7, reviews: 2100, badge: 'Hot Deal',    image: '⌨️' },
    { name: 'Noise Cancel Earbuds',     price: 1799,  originalPrice: 2999,  rating: 4.5, reviews: 3400, badge: 'Sale',        image: '🎵' },
    { name: '4K Webcam Pro',            price: 4299,  originalPrice: 5999,  rating: 4.6, reviews: 654,  badge: '',            image: '📷' },
  ];

  getDiscount(price: number, original: number): number {
    return Math.round(((original - price) / original) * 100);
  }
}